using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Application.Contracts.Runtime;
using TakomiCode.Application.Contracts.Services;
using TakomiCode.Domain.Entities;
using TaskStatus = TakomiCode.Domain.Entities.TaskStatus;

namespace TakomiCode.Application.Services;

public class OrchestratorExecutionEngine : IOrchestratorExecutionEngine
{
    private readonly IOrchestrationRepository _orchestrationRepository;
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ICodexRuntimeAdapter _codexRuntimeAdapter;

    public OrchestratorExecutionEngine(
        IOrchestrationRepository orchestrationRepository,
        IChatSessionRepository chatSessionRepository,
        IWorkspaceRepository workspaceRepository,
        ICodexRuntimeAdapter codexRuntimeAdapter)
    {
        _orchestrationRepository = orchestrationRepository;
        _chatSessionRepository = chatSessionRepository;
        _workspaceRepository = workspaceRepository;
        _codexRuntimeAdapter = codexRuntimeAdapter;
    }

    public async Task<OrchestrationSession> InitiateSessionAsync(
        string workspaceId, 
        string originalPrompt, 
        CancellationToken cancellationToken = default)
    {
        var session = new OrchestrationSession
        {
            SessionId = $"orch-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}",
            WorkspaceId = workspaceId,
            OriginalPrompt = originalPrompt,
            Status = TaskStatus.InProgress
        };

        await _orchestrationRepository.SaveSessionAsync(session, cancellationToken);
        return session;
    }

    public async Task<OrchestrationTask> AddTaskAsync(
        string sessionId, 
        string taskName, 
        string description, 
        string targetMode, 
        string taskFilePath,
        string? executionCommand = null,
        string? parentTaskId = null,
        string? workingDirectoryOverride = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _orchestrationRepository.GetSessionAsync(sessionId, cancellationToken);
        if (session == null) throw new InvalidOperationException($"Session {sessionId} not found.");

        if (!string.IsNullOrWhiteSpace(parentTaskId))
        {
            var parentTask = await _orchestrationRepository.GetTaskAsync(parentTaskId, cancellationToken);
            if (parentTask == null || parentTask.SessionId != sessionId)
            {
                throw new InvalidOperationException($"Parent task {parentTaskId} was not found in session {sessionId}.");
            }
        }

        var task = new OrchestrationTask
        {
            SessionId = sessionId,
            ParentTaskId = parentTaskId,
            Name = taskName,
            Description = description,
            TargetMode = targetMode,
            TaskFilePath = taskFilePath,
            ExecutionCommand = executionCommand,
            WorkingDirectoryOverride = workingDirectoryOverride,
            Status = TaskStatus.Pending
        };

        await _orchestrationRepository.SaveTaskAsync(task, cancellationToken);
        return task;
    }

    public async Task AddDependencyAsync(string taskId, string dependsOnTaskId, CancellationToken cancellationToken = default)
    {
        var task = await _orchestrationRepository.GetTaskAsync(taskId, cancellationToken);
        if (task == null) throw new InvalidOperationException($"Task {taskId} not found.");
        
        if (!task.Dependencies.Contains(dependsOnTaskId))
        {
            task.Dependencies.Add(dependsOnTaskId);
            await _orchestrationRepository.SaveTaskAsync(task, cancellationToken);
        }
    }

    public async Task<OrchestrationRun> StartTaskExecutionAsync(
        string taskId,
        string? parentRunId = null,
        string? chatSessionId = null,
        CancellationToken cancellationToken = default)
    {
        var task = await _orchestrationRepository.GetTaskAsync(taskId, cancellationToken);
        if (task == null) throw new InvalidOperationException($"Task {taskId} not found.");
        var session = await _orchestrationRepository.GetSessionAsync(task.SessionId, cancellationToken);
        if (session == null) throw new InvalidOperationException($"Session {task.SessionId} not found.");

        // Check dependencies
        var dependentTasks = await _orchestrationRepository.GetTasksForSessionAsync(task.SessionId, cancellationToken);
        var unfulfilledDeps = task.Dependencies.Where(depId => 
        {
            var depTask = dependentTasks.FirstOrDefault(t => t.Id == depId);
            return depTask == null || depTask.Status != TaskStatus.Completed;
        }).ToList();

        if (unfulfilledDeps.Any())
        {
            task.Status = TaskStatus.Blocked;
            await _orchestrationRepository.SaveTaskAsync(task, cancellationToken);
            throw new InvalidOperationException($"Task {task.Id} is blocked by dependencies: {string.Join(", ", unfulfilledDeps)}");
        }

        var workspace = await _workspaceRepository.GetWorkspaceAsync(session.WorkspaceId, cancellationToken);
        var parentRun = string.IsNullOrWhiteSpace(parentRunId)
            ? null
            : await _orchestrationRepository.GetRunAsync(parentRunId, cancellationToken);

        ChatSession? linkedChatSession = null;
        if (!string.IsNullOrWhiteSpace(chatSessionId))
        {
            linkedChatSession = await _chatSessionRepository.GetSessionAsync(chatSessionId, cancellationToken);
        }

        if (linkedChatSession is null)
        {
            linkedChatSession = new ChatSession
            {
                WorkspaceId = session.WorkspaceId,
                Title = $"Task: {task.Name}",
                ParentSessionId = parentRun?.ChatSessionId,
                ModeSlug = task.TargetMode,
                WorktreePath = task.WorkingDirectoryOverride ?? workspace?.Path
            };

            await _chatSessionRepository.SaveSessionAsync(linkedChatSession, cancellationToken);
        }

        task.Status = TaskStatus.Queued;
        task.StartedAt ??= DateTimeOffset.UtcNow;

        var run = new OrchestrationRun
        {
            SessionId = session.SessionId,
            WorkspaceId = session.WorkspaceId,
            TaskId = task.Id,
            ParentRunId = parentRunId,
            ChatSessionId = linkedChatSession.Id,
            WorkingDirectory = ResolveWorkingDirectory(task, workspace?.Path),
            Status = TaskStatus.Queued,
            IsBackground = true
        };

        task.ExecutionRuns.Add(run);
        await _orchestrationRepository.SaveRunAsync(run, cancellationToken);
        await _orchestrationRepository.SaveTaskAsync(task, cancellationToken);

        _ = Task.Run(() => ExecuteRunInBackgroundAsync(task.Id, run.RunId), CancellationToken.None);
        return run;
    }
    
    public async Task MarkTaskCompleteAsync(string taskId, string resultFilePath, CancellationToken cancellationToken = default)
    {
        var task = await _orchestrationRepository.GetTaskAsync(taskId, cancellationToken);
        if (task == null) throw new InvalidOperationException($"Task {taskId} not found.");

        task.Status = TaskStatus.Completed;
        task.CompletedAt = DateTimeOffset.UtcNow;

        var activeRun = task.ExecutionRuns.LastOrDefault(r => r.Status is TaskStatus.InProgress or TaskStatus.Queued);
        if (activeRun != null)
        {
            activeRun.Status = TaskStatus.Completed;
            activeRun.CompletedAt = DateTimeOffset.UtcNow;
            activeRun.ResultFilePath = resultFilePath;
            await _orchestrationRepository.SaveRunAsync(activeRun, cancellationToken);

            if (!string.IsNullOrWhiteSpace(resultFilePath) && File.Exists(resultFilePath))
            {
                var artifact = new TaskArtifact
                {
                    SessionId = task.SessionId,
                    TaskId = task.Id,
                    RunId = activeRun.RunId,
                    FilePath = resultFilePath,
                    ArtifactType = "result",
                    Description = "Task Completion Result Document"
                };
                task.Artifacts.Add(artifact);
                await _orchestrationRepository.SaveArtifactAsync(artifact, cancellationToken);
            }
        }

        await _orchestrationRepository.SaveTaskAsync(task, cancellationToken);
    }

    public async Task MarkTaskFailedAsync(string taskId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var task = await _orchestrationRepository.GetTaskAsync(taskId, cancellationToken);
        if (task == null) throw new InvalidOperationException($"Task {taskId} not found.");

        task.Status = TaskStatus.Failed;
        task.CompletedAt = DateTimeOffset.UtcNow;

        var activeRun = task.ExecutionRuns.LastOrDefault(r => r.Status is TaskStatus.InProgress or TaskStatus.Queued);
        if (activeRun != null)
        {
            activeRun.Status = TaskStatus.Failed;
            activeRun.CompletedAt = DateTimeOffset.UtcNow;
            activeRun.ErrorMessage = errorMessage;
            await _orchestrationRepository.SaveRunAsync(activeRun, cancellationToken);
        }

        await _orchestrationRepository.SaveTaskAsync(task, cancellationToken);
    }

    public async Task<TaskArtifact> AttachArtifactAsync(
        string taskId,
        string runId,
        string filePath,
        string description,
        string artifactType = "result",
        CancellationToken cancellationToken = default)
    {
        var task = await _orchestrationRepository.GetTaskAsync(taskId, cancellationToken);
        if (task == null) throw new InvalidOperationException($"Task {taskId} not found.");

        var artifact = new TaskArtifact
        {
            SessionId = task.SessionId,
            TaskId = taskId,
            RunId = runId,
            FilePath = filePath,
            Description = description,
            ArtifactType = artifactType
        };

        task.Artifacts.Add(artifact);
        await _orchestrationRepository.SaveArtifactAsync(artifact, cancellationToken);
        await _orchestrationRepository.SaveTaskAsync(task, cancellationToken);
        return artifact;
    }

    public async Task BackgroundMonitorRunsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var tasks = await _orchestrationRepository.GetTasksForSessionAsync(sessionId, cancellationToken);
        
        var blockedTasks = tasks.Where(t => t.Status == TaskStatus.Blocked).ToList();
        foreach (var task in blockedTasks)
        {
            var unfulfilledDeps = task.Dependencies.Where(depId => 
            {
                var depTask = tasks.FirstOrDefault(t => t.Id == depId);
                return depTask == null || depTask.Status != TaskStatus.Completed;
            }).ToList();

            if (!unfulfilledDeps.Any())
            {
                await StartTaskExecutionAsync(task.Id, cancellationToken: cancellationToken);
            }
        }
        
        tasks = (await _orchestrationRepository.GetTasksForSessionAsync(sessionId, cancellationToken)).ToList();

        var session = await _orchestrationRepository.GetSessionAsync(sessionId, cancellationToken);
        if (session == null)
        {
            return;
        }

        if (tasks.Any(t => t.Status == TaskStatus.Failed))
        {
            session.Status = TaskStatus.Failed;
        }
        else if (tasks.Any(t => t.Status == TaskStatus.Cancelled))
        {
            session.Status = TaskStatus.Cancelled;
            session.CompletedAt ??= DateTimeOffset.UtcNow;
        }
        else if (tasks.All(t => t.Status == TaskStatus.Completed))
        {
            session.Status = TaskStatus.Completed;
            session.CompletedAt ??= DateTimeOffset.UtcNow;
        }
        else if (tasks.Any(t => t.Status is TaskStatus.InProgress or TaskStatus.Queued or TaskStatus.Blocked or TaskStatus.Paused))
        {
            session.Status = TaskStatus.InProgress;
        }

        await _orchestrationRepository.SaveSessionAsync(session, cancellationToken);
    }

    private async Task ExecuteRunInBackgroundAsync(string taskId, string runId)
    {
        try
        {
            var task = await _orchestrationRepository.GetTaskAsync(taskId, CancellationToken.None);
            if (task == null)
            {
                throw new InvalidOperationException($"Task {taskId} not found.");
            }

            var run = await _orchestrationRepository.GetRunAsync(runId, CancellationToken.None);
            if (run == null)
            {
                throw new InvalidOperationException($"Run {runId} not found.");
            }

            await ExecuteRunAsync(task, run, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Background orchestration run '{runId}' failed unexpectedly: {ex}");
            await TryRecordBackgroundFailureAsync(taskId, runId, ex);
        }
    }

    private async Task ExecuteRunAsync(OrchestrationTask task, OrchestrationRun run, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(task.ExecutionCommand))
            {
                run.Status = TaskStatus.Queued;
                await _orchestrationRepository.SaveRunAsync(run, cancellationToken);
                return;
            }

            run.Status = TaskStatus.InProgress;
            await _orchestrationRepository.SaveRunAsync(run, cancellationToken);

            var workspaceId = run.WorkspaceId;
            if (string.IsNullOrWhiteSpace(workspaceId))
            {
                var session = await _orchestrationRepository.GetSessionAsync(run.SessionId, cancellationToken);
                workspaceId = session?.WorkspaceId;
            }

            var runtimeResult = await _codexRuntimeAdapter.StartRunAsync(
                new CodexRunRequest
                {
                    RunId = run.RunId,
                    WorkingDirectory = run.WorkingDirectory ?? Environment.CurrentDirectory,
                    Command = task.ExecutionCommand,
                    WorkspaceId = workspaceId,
                    SessionId = run.SessionId,
                    ChatSessionId = run.ChatSessionId
                },
                cancellationToken);

            if (runtimeResult.FinalState == CodexRuntimeState.Completed)
            {
                var expectedResultPath = ResolveResultFilePath(task.TaskFilePath);
                await MarkTaskCompleteAsync(task.Id, expectedResultPath, cancellationToken);
                return;
            }

            if (runtimeResult.FinalState == CodexRuntimeState.Cancelled)
            {
                await MarkTaskCancelledAsync(task.Id, runtimeResult.ErrorMessage, cancellationToken);
                return;
            }

            await MarkTaskFailedAsync(task.Id, runtimeResult.ErrorMessage ?? "Codex runtime execution failed.", cancellationToken);
        }
        catch (Exception ex)
        {
            await MarkTaskFailedAsync(task.Id, ex.Message, cancellationToken);
        }
    }

    private async Task TryRecordBackgroundFailureAsync(string taskId, string runId, Exception exception)
    {
        try
        {
            var failedAt = DateTimeOffset.UtcNow;

            var run = await _orchestrationRepository.GetRunAsync(runId, CancellationToken.None);
            if (run != null)
            {
                run.Status = TaskStatus.Failed;
                run.CompletedAt = failedAt;
                if (string.IsNullOrWhiteSpace(run.ErrorMessage))
                {
                    run.ErrorMessage = exception.Message;
                }

                await _orchestrationRepository.SaveRunAsync(run, CancellationToken.None);
            }

            var task = await _orchestrationRepository.GetTaskAsync(taskId, CancellationToken.None);
            if (task == null)
            {
                return;
            }

            task.Status = TaskStatus.Failed;
            task.CompletedAt = failedAt;

            var taskRun = task.ExecutionRuns.LastOrDefault(r => string.Equals(r.RunId, runId, StringComparison.Ordinal));
            if (taskRun != null)
            {
                taskRun.Status = TaskStatus.Failed;
                taskRun.CompletedAt = failedAt;
                if (string.IsNullOrWhiteSpace(taskRun.ErrorMessage))
                {
                    taskRun.ErrorMessage = exception.Message;
                }
            }

            await _orchestrationRepository.SaveTaskAsync(task, CancellationToken.None);
        }
        catch (Exception persistenceException)
        {
            Trace.TraceError($"Failed to persist background orchestration error for run '{runId}': {persistenceException}");
        }
    }

    private async Task MarkTaskCancelledAsync(string taskId, string? details, CancellationToken cancellationToken)
    {
        var task = await _orchestrationRepository.GetTaskAsync(taskId, cancellationToken);
        if (task == null)
        {
            return;
        }

        task.Status = TaskStatus.Cancelled;
        task.CompletedAt = DateTimeOffset.UtcNow;

        var activeRun = task.ExecutionRuns.LastOrDefault(r => r.Status is TaskStatus.InProgress or TaskStatus.Queued or TaskStatus.Paused);
        if (activeRun != null)
        {
            activeRun.Status = TaskStatus.Cancelled;
            activeRun.CompletedAt = DateTimeOffset.UtcNow;
            activeRun.ErrorMessage = details;
            await _orchestrationRepository.SaveRunAsync(activeRun, cancellationToken);
        }

        await _orchestrationRepository.SaveTaskAsync(task, cancellationToken);
    }

    private static string ResolveWorkingDirectory(OrchestrationTask task, string? workspacePath)
    {
        if (!string.IsNullOrWhiteSpace(task.WorkingDirectoryOverride))
        {
            return task.WorkingDirectoryOverride;
        }

        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            return workspacePath;
        }

        var taskFileDirectory = Path.GetDirectoryName(task.TaskFilePath);
        return string.IsNullOrWhiteSpace(taskFileDirectory)
            ? Environment.CurrentDirectory
            : taskFileDirectory;
    }

    private static string ResolveResultFilePath(string taskFilePath)
    {
        var normalizedPath = taskFilePath.Replace("\\pending\\", "\\completed\\", StringComparison.OrdinalIgnoreCase);
        return normalizedPath.EndsWith(".task.md", StringComparison.OrdinalIgnoreCase)
            ? normalizedPath[..^8] + ".result.md"
            : normalizedPath + ".result.md";
    }
}
