using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Application.Contracts.Runtime;
using TakomiCode.Domain.Events;

namespace TakomiCode.RuntimeAdapters.Codex;

/// <summary>
/// Development-only cloud runtime adapter that simulates remote Codex execution.
/// It does not talk to a real cloud backend and must not be used as a production runtime.
/// </summary>
public class CodexCloudAdapter : ICodexRuntimeAdapter
{
    public event EventHandler<CodexRuntimeStateEventArgs>? StateChanged;
    public event EventHandler<CodexRuntimeOutputEventArgs>? OutputReceived;

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ConcurrentDictionary<string, CloudRunControl> _activeRuns = new();

    public CodexCloudAdapter(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<CodexRunResult> StartRunAsync(CodexRunRequest request, CancellationToken cancellationToken = default)
    {
        var runControl = new CloudRunControl(request, cancellationToken);
        _activeRuns[request.RunId] = runControl;

        try
        {
            await EmitStateAsync(
                request.RunId,
                CodexRuntimeState.Starting,
                "Initiating mock cloud execution request...",
                runControl.AuditContext,
                runControl.CancellationTokenSource.Token);

            await ControlledDelayAsync(TimeSpan.FromMilliseconds(500), runControl);
            await EmitStateAsync(
                request.RunId,
                CodexRuntimeState.Running,
                "Mock cloud allocation secured. Simulating execution...",
                runControl.AuditContext,
                runControl.CancellationTokenSource.Token);

            OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(request.RunId, "[Cloud Mock] Connecting to Takomi Nexus...", false));
            await ControlledDelayAsync(TimeSpan.FromMilliseconds(1000), runControl);

            await WaitIfPausedAsync(runControl);
            OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(request.RunId, $"[Cloud Mock] Executing command: {request.Command}", false));
            await ControlledDelayAsync(TimeSpan.FromMilliseconds(2000), runControl);

            await WaitIfPausedAsync(runControl);
            OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(request.RunId, "[Cloud Mock] Execution mock complete. Synchronizing output artifacts...", false));

            await EmitStateAsync(
                request.RunId,
                CodexRuntimeState.Completed,
                "Mock cloud execution completed successfully",
                runControl.AuditContext,
                runControl.CancellationTokenSource.Token);

            return new CodexRunResult
            {
                RunId = request.RunId,
                FinalState = CodexRuntimeState.Completed,
                ExitCode = 0
            };
        }
        catch (OperationCanceledException)
        {
            await EmitStateAsync(
                request.RunId,
                CodexRuntimeState.Cancelled,
                "Mock cloud execution cancelled",
                runControl.AuditContext,
                CancellationToken.None);
            return new CodexRunResult
            {
                RunId = request.RunId,
                FinalState = CodexRuntimeState.Cancelled
            };
        }
        catch (Exception ex)
        {
            await EmitStateAsync(
                request.RunId,
                CodexRuntimeState.Failed,
                $"Mock cloud execution error: {ex.Message}",
                runControl.AuditContext,
                CancellationToken.None);
            return new CodexRunResult
            {
                RunId = request.RunId,
                FinalState = CodexRuntimeState.Failed,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            _activeRuns.TryRemove(request.RunId, out _);
            runControl.Dispose();
        }
    }

    public async Task CancelRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (_activeRuns.TryGetValue(runId, out var runControl))
        {
            runControl.CancellationTokenSource.Cancel();
        }
        else
        {
            await EmitStateAsync(
                runId,
                CodexRuntimeState.Cancelled,
                details: "Cancellation requested for an inactive cloud run",
                auditContext: null,
                cancellationToken: cancellationToken);
        }
    }

    public async Task SendInterventionAsync(string runId, TakomiCode.Domain.Entities.InterventionAction action, string? payload = null, CancellationToken cancellationToken = default)
    {
        if (action == TakomiCode.Domain.Entities.InterventionAction.Cancel)
        {
            await CancelRunAsync(runId, cancellationToken);
            return;
        }

        if (action == TakomiCode.Domain.Entities.InterventionAction.Pause)
        {
            if (!_activeRuns.TryGetValue(runId, out var runControl))
            {
                throw new InvalidOperationException($"Cloud run '{runId}' is no longer active.");
            }

            if (!runControl.IsPaused)
            {
                runControl.IsPaused = true;
                await EmitStateAsync(
                    runId,
                    CodexRuntimeState.Paused,
                    "Mock cloud execution paused via intervention",
                    runControl.AuditContext,
                    cancellationToken);
            }

            return;
        }

        if (action == TakomiCode.Domain.Entities.InterventionAction.Resume)
        {
            if (!_activeRuns.TryGetValue(runId, out var runControl))
            {
                throw new InvalidOperationException($"Cloud run '{runId}' is no longer active.");
            }

            if (runControl.IsPaused)
            {
                runControl.IsPaused = false;
                await EmitStateAsync(
                    runId,
                    CodexRuntimeState.Running,
                    "Mock cloud execution resumed via intervention",
                    runControl.AuditContext,
                    cancellationToken);
            }

            return;
        }

        throw new NotSupportedException($"Intervention action '{action}' is not natively supported by the cloud runtime mock.");
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
            await _auditLogRepository.AppendEventAsync(
                new AuditEvent
                {
                    EventType = $"runtime.cloud.{state.ToString().ToLowerInvariant()}",
                    Description = details ?? state.ToString(),
                    SessionId = auditContext?.SessionId,
                    WorkspaceId = auditContext?.WorkspaceId,
                    RunId = auditContext?.RunId ?? runId,
                    ChatSessionId = auditContext?.ChatSessionId
                },
                cancellationToken);
        }
        catch
        {
            // Audit logging must not break the runtime adapter.
        }
    }

    private static async Task ControlledDelayAsync(TimeSpan duration, CloudRunControl runControl)
    {
        var remaining = duration;
        while (remaining > TimeSpan.Zero)
        {
            await WaitIfPausedAsync(runControl);

            var slice = remaining > TimeSpan.FromMilliseconds(100)
                ? TimeSpan.FromMilliseconds(100)
                : remaining;

            await Task.Delay(slice, runControl.CancellationTokenSource.Token);
            remaining -= slice;
        }
    }

    private static async Task WaitIfPausedAsync(CloudRunControl runControl)
    {
        while (runControl.IsPaused)
        {
            await Task.Delay(100, runControl.CancellationTokenSource.Token);
        }
    }

    private sealed class CloudRunControl : IDisposable
    {
        public CloudRunControl(CodexRunRequest request, CancellationToken cancellationToken)
        {
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            AuditContext = AuditContext.FromRequest(request);
        }

        public CancellationTokenSource CancellationTokenSource { get; }
        public AuditContext AuditContext { get; }
        public bool IsPaused { get; set; }

        public void Dispose()
        {
            CancellationTokenSource.Dispose();
        }
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
