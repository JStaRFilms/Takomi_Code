namespace TakomiCode.Domain.Events;

public class AuditEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string? WorkspaceId { get; set; }
    public string? RunId { get; set; }
    public string? ChatSessionId { get; set; }
}
