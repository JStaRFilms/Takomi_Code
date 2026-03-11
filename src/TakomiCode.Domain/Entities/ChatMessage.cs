using System;

namespace TakomiCode.Domain.Entities;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Role { get; set; } = string.Empty; // e.g. "User", "Assistant", "System"
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
