using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Application.Contracts.Services;

public interface IOrchestratorExecutionEngine
{
    Task<OrchestrationSession> InitiateSessionAsync(string workspaceId, string originalPrompt, CancellationToken cancellationToken = default);
    Task<OrchestrationTask> AddTaskAsync(
        string sessionId,
        string taskName,
        string description,
        string targetMode,
        string taskFilePath,
        string? executionCommand = null,
        string? parentTaskId = null,
        string? workingDirectoryOverride = null,
        CancellationToken cancellationToken = default);
    Task<OrchestrationRun> StartTaskExecutionAsync(string taskId, string? parentRunId = null, CancellationToken cancellationToken = default);
    Task BackgroundMonitorRunsAsync(string sessionId, CancellationToken cancellationToken = default);
    Task MarkTaskCompleteAsync(string taskId, string resultFilePath, CancellationToken cancellationToken = default);
    Task MarkTaskFailedAsync(string taskId, string errorMessage, CancellationToken cancellationToken = default);
    Task AddDependencyAsync(string taskId, string dependsOnTaskId, CancellationToken cancellationToken = default);
    Task<TaskArtifact> AttachArtifactAsync(
        string taskId,
        string runId,
        string filePath,
        string description,
        string artifactType = "result",
        CancellationToken cancellationToken = default);
}
