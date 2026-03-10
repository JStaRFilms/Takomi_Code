using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Domain.Events;

namespace TakomiCode.Infrastructure.Persistence;

public class LocalAuditLogRepository : IAuditLogRepository
{
    private readonly List<AuditEvent> _inMemoryStore = new();

    public Task AppendEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        _inMemoryStore.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AuditEvent>> GetEventsBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var result = _inMemoryStore.Where(e => e.SessionId == sessionId);
        return Task.FromResult(result);
    }
}
