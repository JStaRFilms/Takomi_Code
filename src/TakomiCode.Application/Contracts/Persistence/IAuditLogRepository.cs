using TakomiCode.Domain.Events;

namespace TakomiCode.Application.Contracts.Persistence;

public interface IAuditLogRepository
{
    Task AppendEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEvent>> GetEventsBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEvent>> GetEventsByWorkspaceIdAsync(string workspaceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEvent>> GetEventsByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}
