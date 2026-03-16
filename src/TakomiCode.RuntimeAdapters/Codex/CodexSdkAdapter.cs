using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Application.Contracts.Runtime;
using TakomiCode.Domain.Events;

namespace TakomiCode.RuntimeAdapters.Codex;

public class CodexSdkAdapter : ICodexRuntimeAdapter
{
    public event EventHandler<CodexRuntimeStateEventArgs>? StateChanged;
    public event EventHandler<CodexRuntimeOutputEventArgs>? OutputReceived;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ConcurrentDictionary<string, Process> _runningProcesses = new();
    private readonly ConcurrentDictionary<string, AuditContext> _runAuditContexts = new();
    private readonly ConcurrentDictionary<string, byte> _cancelledRuns = new();

    public CodexSdkAdapter(IAuditLogRepository auditLogRepository)
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

        var bridgeScriptPath = ResolveBridgeScriptPath();
        if (!File.Exists(bridgeScriptPath))
        {
            return await FailRunAsync(request.RunId, $"Codex SDK bridge not found: {bridgeScriptPath}", auditContext, cancellationToken);
        }

        var requestPayloadPath = await WriteRequestPayloadAsync(request, cancellationToken);
        Process? process = null;
        CancellationTokenRegistration cancellationRegistration = default;
        string? bridgeFailureMessage = null;
        string? finalResponse = null;

        await EmitStateAsync(request.RunId, CodexRuntimeState.Starting, auditContext: auditContext, cancellationToken: cancellationToken);

        try
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"{QuoteArgument(bridgeScriptPath)} --request {QuoteArgument(requestPayloadPath)}",
                    WorkingDirectory = request.WorkingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            var started = process.Start();
            if (!started)
            {
                throw new InvalidOperationException("Failed to start the Codex SDK bridge process.");
            }

            _runningProcesses[request.RunId] = process;
            cancellationRegistration = cancellationToken.Register(() => MarkCancelledAndTerminateProcess(request.RunId, process));

            var stdoutTask = ReadOutputAsync(
                process.StandardOutput,
                request.RunId,
                onCompleted: response => finalResponse = response,
                onFailure: message => bridgeFailureMessage = message,
                cancellationToken);

            var stderrTask = ReadErrorAsync(process.StandardError, request.RunId, cancellationToken);

            await EmitStateAsync(
                request.RunId,
                CodexRuntimeState.Running,
                details: "Codex SDK bridge running",
                auditContext: auditContext,
                cancellationToken: cancellationToken);

            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(stdoutTask, stderrTask);

            if (_cancelledRuns.TryRemove(request.RunId, out _))
            {
                await EmitStateAsync(
                    request.RunId,
                    CodexRuntimeState.Cancelled,
                    details: "Run cancelled before completion",
                    auditContext: auditContext,
                    cancellationToken: CancellationToken.None);

                return new CodexRunResult
                {
                    RunId = request.RunId,
                    FinalState = CodexRuntimeState.Cancelled,
                    ExitCode = process.ExitCode
                };
            }

            if (!string.IsNullOrWhiteSpace(bridgeFailureMessage))
            {
                return await FailRunAsync(
                    request.RunId,
                    bridgeFailureMessage,
                    auditContext,
                    CancellationToken.None,
                    process.ExitCode);
            }

            if (process.ExitCode != 0)
            {
                return await FailRunAsync(
                    request.RunId,
                    $"Codex SDK bridge exited with code {process.ExitCode}",
                    auditContext,
                    CancellationToken.None,
                    process.ExitCode);
            }

            await EmitStateAsync(
                request.RunId,
                CodexRuntimeState.Completed,
                details: string.IsNullOrWhiteSpace(finalResponse) ? "Run completed successfully" : finalResponse,
                auditContext: auditContext,
                cancellationToken: cancellationToken);

            return new CodexRunResult
            {
                RunId = request.RunId,
                FinalState = CodexRuntimeState.Completed,
                ExitCode = process.ExitCode
            };
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

            try
            {
                File.Delete(requestPayloadPath);
            }
            catch
            {
                // Best-effort cleanup only.
            }
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
            details: "Cancellation requested for an inactive SDK run",
            auditContext: TryGetAuditContext(runId),
            cancellationToken: cancellationToken);
    }

    public async Task SendInterventionAsync(
        string runId,
        TakomiCode.Domain.Entities.InterventionAction action,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        if (action == TakomiCode.Domain.Entities.InterventionAction.Cancel)
        {
            await CancelRunAsync(runId, cancellationToken);
            return;
        }

        throw new NotSupportedException($"Intervention action '{action}' is not natively supported by the Codex SDK runtime.");
    }

    private async Task ReadOutputAsync(
        StreamReader reader,
        string runId,
        Action<string> onCompleted,
        Action<string> onFailure,
        CancellationToken cancellationToken)
    {
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!TryParseBridgeEvent(line, out var bridgeEvent))
            {
                OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(runId, line, false, CodexRuntimeOutputKind.Progress));
                continue;
            }

            switch (bridgeEvent.Type)
            {
                case "progress" when !string.IsNullOrWhiteSpace(bridgeEvent.Message):
                    OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(runId, bridgeEvent.Message, false, CodexRuntimeOutputKind.Progress));
                    break;
                case "assistant_stream" when !string.IsNullOrWhiteSpace(bridgeEvent.Message):
                    OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(runId, bridgeEvent.Message, false, CodexRuntimeOutputKind.AssistantStream));
                    break;
                case "completed" when !string.IsNullOrWhiteSpace(bridgeEvent.FinalResponse):
                    OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(runId, bridgeEvent.FinalResponse, false, CodexRuntimeOutputKind.AssistantFinal));
                    onCompleted(bridgeEvent.FinalResponse);
                    break;
                case "failed" when !string.IsNullOrWhiteSpace(bridgeEvent.Message):
                    onFailure(bridgeEvent.Message);
                    OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(runId, bridgeEvent.Message, true));
                    break;
            }
        }
    }

    private async Task ReadErrorAsync(StreamReader reader, string runId, CancellationToken cancellationToken)
    {
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(runId, line, true));
        }
    }

    private async Task<CodexRunResult> FailRunAsync(
        string runId,
        string errorMessage,
        AuditContext? auditContext,
        CancellationToken cancellationToken,
        int? exitCode = null)
    {
        await EmitStateAsync(runId, CodexRuntimeState.Failed, errorMessage, auditContext, cancellationToken);
        return new CodexRunResult
        {
            RunId = runId,
            FinalState = CodexRuntimeState.Failed,
            ExitCode = exitCode,
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

        if (string.IsNullOrWhiteSpace(request.WorkingDirectory) || !Directory.Exists(request.WorkingDirectory))
        {
            return $"Working directory does not exist: {request.WorkingDirectory}";
        }

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return "Prompt is required";
        }

        return null;
    }

    private static string ResolveBridgeScriptPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var bridgePath = Path.Combine(current.FullName, "tools", "codex-sdk-bridge", "index.mjs");
            if (File.Exists(bridgePath))
            {
                return bridgePath;
            }

            current = current.Parent;
        }

        return Path.Combine(Environment.CurrentDirectory, "tools", "codex-sdk-bridge", "index.mjs");
    }

    private static async Task<string> WriteRequestPayloadAsync(CodexRunRequest request, CancellationToken cancellationToken)
    {
        var requestPath = Path.Combine(
            Path.GetTempPath(),
            $"takomi-codex-sdk-{request.RunId}.json");

        var payload = new BridgeRunRequest
        {
            RunId = request.RunId,
            WorkspaceId = request.WorkspaceId,
            SessionId = request.SessionId,
            ChatSessionId = request.ChatSessionId,
            WorkingDirectory = request.WorkingDirectory,
            Prompt = request.Command
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        await File.WriteAllTextAsync(requestPath, json, cancellationToken);
        return requestPath;
    }

    private static string QuoteArgument(string value)
    {
        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private AuditContext? TryGetAuditContext(string runId)
    {
        _runAuditContexts.TryGetValue(runId, out var auditContext);
        return auditContext;
    }

    private static bool TryParseBridgeEvent(string json, out BridgeEvent payload)
    {
        try
        {
            payload = JsonSerializer.Deserialize<BridgeEvent>(json, SerializerOptions) ?? new BridgeEvent();
            return !string.IsNullOrWhiteSpace(payload.Type);
        }
        catch
        {
            payload = new BridgeEvent();
            return false;
        }
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
            // Best-effort only.
        }
    }

    private sealed class BridgeRunRequest
    {
        public string RunId { get; init; } = string.Empty;
        public string? WorkspaceId { get; init; }
        public string? SessionId { get; init; }
        public string? ChatSessionId { get; init; }
        public string WorkingDirectory { get; init; } = string.Empty;
        public string Prompt { get; init; } = string.Empty;
    }

    private sealed class BridgeEvent
    {
        public string Type { get; init; } = string.Empty;
        public string? Message { get; init; }
        public string? FinalResponse { get; init; }
        public string? ThreadId { get; init; }
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
