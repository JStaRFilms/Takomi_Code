using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Application.Contracts.Runtime;
using TakomiCode.Domain.Events;

namespace TakomiCode.RuntimeAdapters.Codex;

public class CodexCliAdapter : ICodexRuntimeAdapter
{
    public event EventHandler<CodexRuntimeStateEventArgs>? StateChanged;
    public event EventHandler<CodexRuntimeOutputEventArgs>? OutputReceived;

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ConcurrentDictionary<string, Process> _runningProcesses = new();
    private readonly ConcurrentDictionary<string, AuditContext> _runAuditContexts = new();
    private readonly ConcurrentDictionary<string, byte> _cancelledRuns = new();

    public CodexCliAdapter(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<CodexRunResult> StartRunAsync(CodexRunRequest request, CancellationToken cancellationToken = default)
    {
        var auditContext = AuditContext.FromRequest(request);
        _runAuditContexts[request.RunId] = auditContext;

        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return await FailRunAsync(request.RunId, validationError, auditContext, cancellationToken);
        }

        await EmitStateAsync(request.RunId, CodexRuntimeState.Starting, auditContext: auditContext, cancellationToken: cancellationToken);

        Process? process = null;
        CancellationTokenRegistration cancellationRegistration = default;
        var errorLines = new ConcurrentQueue<string>();
        string? authFailureMessage = null;

        try
        {
            var codexExecutable = await ResolveCodexExecutableAsync(cancellationToken);
            if (codexExecutable is null)
            {
                return await FailRunAsync(
                    request.RunId,
                    "Codex CLI is not installed or not available on PATH",
                    auditContext,
                    cancellationToken);
            }

            var startInfo = CreateStartInfo(codexExecutable, request);

            foreach (var envVar in request.EnvironmentVariables)
            {
                startInfo.Environment[envVar.Key] = envVar.Value;
            }

            process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(request.RunId, e.Data, false));
                    if (IsAuthenticationFailure(e.Data))
                    {
                        authFailureMessage = "Authentication issue detected in Codex CLI output";
                        TryTerminateProcess(process);
                    }
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    errorLines.Enqueue(e.Data);
                    OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(request.RunId, e.Data, true));
                    if (IsAuthenticationFailure(e.Data))
                    {
                        authFailureMessage = "Authentication issue detected in Codex CLI stderr";
                        TryTerminateProcess(process);
                    }
                }
            };
            
            bool started = process.Start();
            if (!started)
            {
                throw new InvalidOperationException("Failed to start process.");
            }

            _runningProcesses[request.RunId] = process;
            cancellationRegistration = cancellationToken.Register(() => MarkCancelledAndTerminateProcess(request.RunId, process));
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await EmitStateAsync(
                request.RunId,
                CodexRuntimeState.Running,
                details: $"Launched '{Path.GetFileName(codexExecutable)}'",
                auditContext: auditContext,
                cancellationToken: cancellationToken);

            await process.WaitForExitAsync(CancellationToken.None);
            return await BuildRunResultAsync(request.RunId, process.ExitCode, authFailureMessage, errorLines, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _cancelledRuns[request.RunId] = 0;
            TryTerminateProcess(process);
            await EmitStateAsync(
                request.RunId,
                CodexRuntimeState.Cancelled,
                auditContext: auditContext,
                cancellationToken: CancellationToken.None);
            return new CodexRunResult
            {
                RunId = request.RunId,
                FinalState = CodexRuntimeState.Cancelled
            };
        }
        catch (Exception ex)
        {
            return await FailRunAsync(request.RunId, ex.Message, auditContext, CancellationToken.None);
        }
        finally
        {
            cancellationRegistration.Dispose();
            _runningProcesses.TryRemove(request.RunId, out _);
            _runAuditContexts.TryRemove(request.RunId, out _);
            process?.Dispose();
        }
    }

    public async Task CancelRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (_runningProcesses.TryGetValue(runId, out var process) && !process.HasExited)
        {
            MarkCancelledAndTerminateProcess(runId, process);
            return;
        }

        await EmitStateAsync(
            runId,
            CodexRuntimeState.Cancelled,
            details: "Cancellation requested for an inactive run",
            auditContext: TryGetAuditContext(runId),
            cancellationToken: cancellationToken);
    }

    public async Task SendInterventionAsync(string runId, TakomiCode.Domain.Entities.InterventionAction action, string? payload = null, CancellationToken cancellationToken = default)
    {
        if (action == TakomiCode.Domain.Entities.InterventionAction.Cancel)
        {
            await CancelRunAsync(runId, cancellationToken);
            return;
        }

        throw new NotSupportedException($"Intervention action '{action}' is not natively supported by the Codex CLI runtime.");
    }

    private async Task<CodexRunResult> BuildRunResultAsync(
        string runId,
        int exitCode,
        string? authFailureMessage,
        ConcurrentQueue<string> errorLines,
        CancellationToken cancellationToken)
    {
        if (_cancelledRuns.TryRemove(runId, out _))
        {
            await EmitStateAsync(
                runId,
                CodexRuntimeState.Cancelled,
                details: "Run cancelled before completion",
                auditContext: TryGetAuditContext(runId),
                cancellationToken: cancellationToken);

            return new CodexRunResult
            {
                RunId = runId,
                FinalState = CodexRuntimeState.Cancelled,
                ExitCode = exitCode
            };
        }

        if (!string.IsNullOrWhiteSpace(authFailureMessage))
        {
            var auditContext = TryGetAuditContext(runId);
            await EmitStateAsync(runId, CodexRuntimeState.Failed, authFailureMessage, auditContext, cancellationToken);
            return new CodexRunResult
            {
                RunId = runId,
                FinalState = CodexRuntimeState.Failed,
                ExitCode = exitCode,
                ErrorMessage = authFailureMessage
            };
        }

        if (exitCode == 0)
        {
            await EmitStateAsync(
                runId,
                CodexRuntimeState.Completed,
                details: "Process exited successfully",
                auditContext: TryGetAuditContext(runId),
                cancellationToken: cancellationToken);

            return new CodexRunResult
            {
                RunId = runId,
                FinalState = CodexRuntimeState.Completed,
                ExitCode = exitCode
            };
        }

        var errorMessage = BuildExecutionFailureMessage(exitCode, errorLines);
        await EmitStateAsync(runId, CodexRuntimeState.Failed, errorMessage, TryGetAuditContext(runId), cancellationToken);

        return new CodexRunResult
        {
            RunId = runId,
            FinalState = CodexRuntimeState.Failed,
            ExitCode = exitCode,
            ErrorMessage = errorMessage
        };
    }

    private async Task<CodexRunResult> FailRunAsync(
        string runId,
        string errorMessage,
        AuditContext? auditContext,
        CancellationToken cancellationToken)
    {
        await EmitStateAsync(runId, CodexRuntimeState.Failed, errorMessage, auditContext, cancellationToken);
        return new CodexRunResult
        {
            RunId = runId,
            FinalState = CodexRuntimeState.Failed,
            ErrorMessage = errorMessage
        };
    }

    private async Task EmitStateAsync(
        string runId,
        CodexRuntimeState state,
        string? details = null,
        AuditContext? auditContext = null,
        CancellationToken cancellationToken = default)
    {
        StateChanged?.Invoke(this, new CodexRuntimeStateEventArgs(runId, state, details));

        try
        {
            var resolvedAuditContext = auditContext ?? TryGetAuditContext(runId);
            await _auditLogRepository.AppendEventAsync(
                new AuditEvent
                {
                    EventType = $"runtime.{state.ToString().ToLowerInvariant()}",
                    Description = details ?? state.ToString(),
                    SessionId = resolvedAuditContext?.SessionId,
                    WorkspaceId = resolvedAuditContext?.WorkspaceId,
                    RunId = resolvedAuditContext?.RunId ?? runId,
                    ChatSessionId = resolvedAuditContext?.ChatSessionId
                },
                cancellationToken);
        }
        catch
        {
            // Audit logging must not break the runtime adapter.
        }
    }

    private static string? ValidateRequest(CodexRunRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RunId))
        {
            return "RunId is required";
        }

        var workingDirectory = ResolveWorkingDirectory(request.WorkingDirectory);
        if (!Directory.Exists(workingDirectory))
        {
            return $"Working directory does not exist: {workingDirectory}";
        }

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return "Command is required";
        }

        return null;
    }

    private AuditContext? TryGetAuditContext(string runId)
    {
        _runAuditContexts.TryGetValue(runId, out var auditContext);
        return auditContext;
    }

    private static string ResolveWorkingDirectory(string? workingDirectory)
    {
        return string.IsNullOrWhiteSpace(workingDirectory)
            ? Environment.CurrentDirectory
            : workingDirectory;
    }

    private async Task<string?> ResolveCodexExecutableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = "codex",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return null;
            }

            var candidates = output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return candidates.FirstOrDefault(path => path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                ?? candidates.FirstOrDefault(path => path.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
                ?? candidates.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static ProcessStartInfo CreateStartInfo(string executablePath, CodexRunRequest request)
    {
        var workingDirectory = ResolveWorkingDirectory(request.WorkingDirectory);
        if (RequiresCommandShell(executablePath))
        {
            return new ProcessStartInfo
            {
                FileName = ResolveCommandShell(),
                Arguments = BuildShellInvocation(executablePath, request.Command),
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        return new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = request.Command,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    private static bool RequiresCommandShell(string executablePath)
    {
        var extension = Path.GetExtension(executablePath);
        return string.Equals(extension, ".cmd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveCommandShell()
    {
        return Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
    }

    private static string BuildShellInvocation(string executablePath, string command)
    {
        var invocation = $"\"{executablePath}\"";
        if (!string.IsNullOrWhiteSpace(command))
        {
            invocation = $"{invocation} {command}";
        }

        return $"/d /s /c \"{invocation}\"";
    }

    private static bool IsAuthenticationFailure(string outputLine)
    {
        return outputLine.Contains("Authentication required", StringComparison.OrdinalIgnoreCase)
            || outputLine.Contains("Not logged in", StringComparison.OrdinalIgnoreCase)
            || outputLine.Contains("Please login", StringComparison.OrdinalIgnoreCase)
            || outputLine.Contains("token expired", StringComparison.OrdinalIgnoreCase);
    }

    private void MarkCancelledAndTerminateProcess(string runId, Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        _cancelledRuns[runId] = 0;
        TryTerminateProcess(process);
    }

    private static void TryTerminateProcess(Process? process)
    {
        if (process is null || process.HasExited)
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort shutdown.
        }
    }

    private static string BuildExecutionFailureMessage(int exitCode, ConcurrentQueue<string> errorLines)
    {
        var detail = string.Join(Environment.NewLine, errorLines.Take(3));
        if (string.IsNullOrWhiteSpace(detail))
        {
            return $"Process exited with code {exitCode}";
        }

        return $"Process exited with code {exitCode}: {detail}";
    }

    private sealed class AuditContext
    {
        public string? WorkspaceId { get; init; }
        public string? SessionId { get; init; }
        public string? ChatSessionId { get; init; }
        public string RunId { get; init; } = string.Empty;

        public static AuditContext FromRequest(CodexRunRequest request)
        {
            return new AuditContext
            {
                WorkspaceId = request.WorkspaceId,
                SessionId = request.SessionId,
                ChatSessionId = request.ChatSessionId,
                RunId = request.RunId
            };
        }
    }
}
