using System;
using System.Collections.Generic;

namespace TakomiCode.Domain.Entities;

public class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string WorkspaceId { get; set; } = string.Empty; // Scoped to a project (Workspace)
    public string? ParentSessionId { get; set; } // Linked to parent session for orchestration
    
    public string Title { get; set; } = "New Chat";
    
    public string? ModeSlug { get; set; } // Associated Takomi mode
    public string? WorktreePath { get; set; } // Explicit worktree override for this session
    
    public List<ChatMessage> Transcript { get; set; } = new();
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
