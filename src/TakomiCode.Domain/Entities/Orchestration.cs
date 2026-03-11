using System;
using System.Collections.Generic;

namespace TakomiCode.Domain.Entities;

public enum TaskStatus
{
    Pending,
    Queued,
    InProgress,
    Paused,
    Completed,
    Cancelled,
    Failed,
    Blocked
}

public class OrchestrationRun
{
    public string RunId { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public string? ParentRunId { get; set; }
    public string? ChatSessionId { get; set; } // Identifies the linked ChatSession
    public bool IsBackground { get; set; } = true;
    public string? WorkingDirectory { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public string? ResultFilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

public class TaskArtifact
{
    public string ArtifactId { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public string RunId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ArtifactType { get; set; } = "result";
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class OrchestrationTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string? ParentTaskId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TaskFilePath { get; set; } = string.Empty;
    public string TargetMode { get; set; } = string.Empty;
    public string? ExecutionCommand { get; set; }
    public string? WorkingDirectoryOverride { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public List<string> Dependencies { get; set; } = new(); // IDs of dependent tasks
    public List<OrchestrationRun> ExecutionRuns { get; set; } = new();
    public List<TaskArtifact> Artifacts { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class OrchestrationSession
{
    public string SessionId { get; set; } = string.Empty; // e.g., orch-20260310-223000
    public string WorkspaceId { get; set; } = string.Empty;
    public string OriginalPrompt { get; set; } = string.Empty;
    public List<OrchestrationTask> Tasks { get; set; } = new();
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
