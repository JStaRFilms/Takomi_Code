using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Application.Contracts.Persistence;

public interface IOrchestrationRepository
{
    Task<OrchestrationSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrchestrationSession>> GetSessionsByWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);
    Task SaveSessionAsync(OrchestrationSession session, CancellationToken cancellationToken = default);
    
    Task<OrchestrationTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrchestrationTask>> GetTasksForSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task SaveTaskAsync(OrchestrationTask task, CancellationToken cancellationToken = default);
    
    Task<OrchestrationRun?> GetRunAsync(string runId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrchestrationRun>> GetRunsForTaskAsync(string taskId, CancellationToken cancellationToken = default);
    Task SaveRunAsync(OrchestrationRun run, CancellationToken cancellationToken = default);

    Task<IEnumerable<TaskArtifact>> GetArtifactsForTaskAsync(string taskId, CancellationToken cancellationToken = default);
    Task SaveArtifactAsync(TaskArtifact artifact, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrchestrationRun>> GetActiveRunsAsync(CancellationToken cancellationToken = default);
}
