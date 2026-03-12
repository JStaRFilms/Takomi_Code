using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Application.Contracts.Runtime;
using TakomiCode.Domain.Events;

namespace TakomiCode.RuntimeAdapters.Codex;

public class CodexCloudAdapter : ICodexRuntimeAdapter
{
    public event EventHandler<CodexRuntimeStateEventArgs>? StateChanged;
    public event EventHandler<CodexRuntimeOutputEventArgs>? OutputReceived;

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeRuns = new();

    public CodexCloudAdapter(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<CodexRunResult> StartRunAsync(CodexRunRequest request, CancellationToken cancellationToken = default)
    {
        await EmitStateAsync(request.RunId, CodexRuntimeState.Starting, "Initiating cloud execution request...", cancellationToken);

        var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeRuns[request.RunId] = runCts;

        try
        {
            await Task.Delay(500, runCts.Token);
            await EmitStateAsync(request.RunId, CodexRuntimeState.Running, "Cloud allocation secured. Executing...", runCts.Token);

            OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(request.RunId, "[Cloud] Connecting to Takomi Nexus...", false));
            await Task.Delay(1000, runCts.Token);

            OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(request.RunId, $"[Cloud] Executing command: {request.Command}", false));
            await Task.Delay(2000, runCts.Token);

            OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(request.RunId, "[Cloud] Execution mock complete. Synchronizing output artifacts...", false));

            await EmitStateAsync(request.RunId, CodexRuntimeState.Completed, "Cloud execution completed successfully", runCts.Token);

            return new CodexRunResult
            {
                RunId = request.RunId,
                FinalState = CodexRuntimeState.Completed,
                ExitCode = 0
            };
        }
        catch (OperationCanceledException)
        {
            await EmitStateAsync(request.RunId, CodexRuntimeState.Cancelled, "Cloud execution cancelled", CancellationToken.None);
            return new CodexRunResult
            {
                RunId = request.RunId,
                FinalState = CodexRuntimeState.Cancelled
            };
        }
        catch (Exception ex)
        {
            await EmitStateAsync(request.RunId, CodexRuntimeState.Failed, $"Cloud execution error: {ex.Message}", CancellationToken.None);
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
            runCts.Dispose();
        }
    }

    public async Task CancelRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (_activeRuns.TryGetValue(runId, out var cts))
        {
            cts.Cancel();
        }
        else
        {
            await EmitStateAsync(
                runId,
                CodexRuntimeState.Cancelled,
                details: "Cancellation requested for an inactive cloud run",
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
            await EmitStateAsync(runId, CodexRuntimeState.Paused, "Cloud execution paused via intervention", cancellationToken);
            return;
        }

        if (action == TakomiCode.Domain.Entities.InterventionAction.Resume)
        {
            await EmitStateAsync(runId, CodexRuntimeState.Running, "Cloud execution resumed via intervention", cancellationToken);
            return;
        }

        throw new NotSupportedException($"Intervention action '{action}' is not natively supported by the cloud runtime mock.");
    }

    private async Task EmitStateAsync(
        string runId,
        CodexRuntimeState state,
        string? details = null,
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
                    SessionId = runId
                },
                cancellationToken);
        }
        catch
        {
            // Audit logging must not break the runtime adapter.
        }
    }
}
