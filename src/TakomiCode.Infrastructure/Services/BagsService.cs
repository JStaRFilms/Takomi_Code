using System;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Application.Contracts.Services;
using TakomiCode.Domain.Events;

namespace TakomiCode.Infrastructure.Services;

public class BagsService : IBagsService
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public BagsService(IWorkspaceRepository workspaceRepository, IAuditLogRepository auditLogRepository)
    {
        _workspaceRepository = workspaceRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task LinkTokenToWorkspaceAsync(string workspaceId, string tokenAddress, CancellationToken cancellationToken = default)
    {
        var workspace = await _workspaceRepository.GetWorkspaceAsync(workspaceId, cancellationToken);
        if (workspace is null)
        {
            throw new InvalidOperationException($"Workspace '{workspaceId}' was not found.");
        }

        var normalizedTokenAddress = tokenAddress.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTokenAddress))
        {
            throw new InvalidOperationException("Bags token address is required.");
        }

        workspace.BagsTokenAddress = normalizedTokenAddress;
        workspace.IsVerificationReady = false; // Reset on new token

        await _workspaceRepository.SaveWorkspaceAsync(workspace, cancellationToken);

        var audit = new AuditEvent
        {
            WorkspaceId = workspaceId,
            EventType = "bags.token_linked",
            Description = $"Linked Bags token address '{normalizedTokenAddress}' to workspace '{workspaceId}'."
        };
        await _auditLogRepository.AppendEventAsync(audit, cancellationToken);
    }

    public async Task<bool> CheckVerificationReadinessAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var workspace = await _workspaceRepository.GetWorkspaceAsync(workspaceId, cancellationToken);
        if (workspace is null)
        {
            throw new InvalidOperationException($"Workspace '{workspaceId}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(workspace.BagsTokenAddress))
        {
            workspace.IsVerificationReady = false;
        }
        else
        {
            // Simulate a Bags API readiness check with a minimal token-shape requirement.
            workspace.IsVerificationReady = workspace.BagsTokenAddress.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                && workspace.BagsTokenAddress.Length >= 10;
        }

        await _workspaceRepository.SaveWorkspaceAsync(workspace, cancellationToken);

        var audit = new AuditEvent
        {
            WorkspaceId = workspaceId,
            EventType = "bags.verification_checked",
            Description = $"Checked Bags verification readiness for workspace '{workspaceId}': {workspace.IsVerificationReady}."
        };
        await _auditLogRepository.AppendEventAsync(audit, cancellationToken);

        return workspace.IsVerificationReady;
    }
}
