using System;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Application.Contracts.Runtime;
using TakomiCode.Application.Contracts.Services;
using TakomiCode.Domain.Entities;
using TakomiCode.Domain.Events;
using TaskStatus = TakomiCode.Domain.Entities.TaskStatus;

namespace TakomiCode.Application.Services;

public class InterventionCommandHandler : IInterventionCommandHandler
{
    private readonly IOrchestrationRepository _orchestrationRepository;
    private readonly ICodexRuntimeAdapter _codexRuntimeAdapter;
    private readonly IAuditLogRepository _auditLogRepository;

    public InterventionCommandHandler(
        IOrchestrationRepository orchestrationRepository,
        ICodexRuntimeAdapter codexRuntimeAdapter,
        IAuditLogRepository auditLogRepository)
    {
        _orchestrationRepository = orchestrationRepository;
        _codexRuntimeAdapter = codexRuntimeAdapter;
        _auditLogRepository = auditLogRepository;
    }

    public async Task ExecuteInterventionAsync(string runId, InterventionAction action, string? payload = null, CancellationToken cancellationToken = default)
    {
        var run = await _orchestrationRepository.GetRunAsync(runId, cancellationToken);
        if (run == null)
            throw new InvalidOperationException($"Run {runId} not found.");

        if (run.Status is not (TaskStatus.InProgress or TaskStatus.Queued or TaskStatus.Paused))
            throw new InvalidOperationException($"Run {runId} is not active. Current status: {run.Status}.");

        await AppendAuditEventAsync(run, "intervention.requested", BuildDescription(action, payload), cancellationToken);

        try
        {
            await _codexRuntimeAdapter.SendInterventionAsync(runId, action, payload, cancellationToken);
            await ApplySupportedInterventionStateAsync(run, action, payload, cancellationToken);
            await AppendAuditEventAsync(run, "intervention.applied", BuildDescription(action, payload), cancellationToken);
        }
        catch (NotSupportedException ex)
        {
            await AppendAuditEventAsync(run, "intervention.unsupported", ex.Message, cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            await AppendAuditEventAsync(run, "intervention.failed", ex.Message, cancellationToken);
            throw;
        }
    }

    private async Task ApplySupportedInterventionStateAsync(
        OrchestrationRun run,
        InterventionAction action,
        string? payload,
        CancellationToken cancellationToken)
    {
        switch (action)
        {
            case InterventionAction.Cancel:
                run.Status = TaskStatus.Cancelled;
                run.CompletedAt = DateTimeOffset.UtcNow;
                run.ErrorMessage = payload ?? "Cancelled by user intervention.";
                await _orchestrationRepository.SaveRunAsync(run, cancellationToken);
                await UpdateTaskStatusAsync(run.TaskId, TaskStatus.Cancelled, run.ErrorMessage, cancellationToken);
                break;
            case InterventionAction.Pause:
                run.Status = TaskStatus.Paused;
                await _orchestrationRepository.SaveRunAsync(run, cancellationToken);
                await UpdateTaskStatusAsync(run.TaskId, TaskStatus.Paused, "Paused by user intervention.", cancellationToken);
                break;
            case InterventionAction.Resume:
                run.Status = TaskStatus.InProgress;
                await _orchestrationRepository.SaveRunAsync(run, cancellationToken);
                await UpdateTaskStatusAsync(run.TaskId, TaskStatus.InProgress, "Resumed by user intervention.", cancellationToken);
                break;
        }
    }

    private async Task UpdateTaskStatusAsync(
        string taskId,
        TaskStatus status,
        string? details,
        CancellationToken cancellationToken)
    {
        var task = await _orchestrationRepository.GetTaskAsync(taskId, cancellationToken);
        if (task == null)
        {
            return;
        }

        task.Status = status;
        if (status is TaskStatus.Cancelled or TaskStatus.Failed or TaskStatus.Completed)
        {
            task.CompletedAt = DateTimeOffset.UtcNow;
        }

        var latestRun = task.ExecutionRuns.LastOrDefault();
        if (latestRun != null)
        {
            latestRun.Status = status;
            latestRun.ErrorMessage = details;
            if (status is TaskStatus.Cancelled or TaskStatus.Failed or TaskStatus.Completed)
            {
                latestRun.CompletedAt = DateTimeOffset.UtcNow;
            }
        }

        await _orchestrationRepository.SaveTaskAsync(task, cancellationToken);
    }

    private async Task AppendAuditEventAsync(OrchestrationRun run, string eventType, string description, CancellationToken cancellationToken)
    {
        var workspaceId = run.WorkspaceId;
        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            var session = await _orchestrationRepository.GetSessionAsync(run.SessionId, cancellationToken);
            workspaceId = session?.WorkspaceId;
        }

        await _auditLogRepository.AppendEventAsync(new AuditEvent
        {
            EventType = eventType,
            Description = description,
            SessionId = run.SessionId,
            WorkspaceId = workspaceId,
            RunId = run.RunId,
            ChatSessionId = run.ChatSessionId
        }, cancellationToken);
    }

    private static string BuildDescription(InterventionAction action, string? payload)
    {
        return $"Intervention: {action}{(string.IsNullOrWhiteSpace(payload) ? string.Empty : $" | Payload: {payload}")}";
    }
}
