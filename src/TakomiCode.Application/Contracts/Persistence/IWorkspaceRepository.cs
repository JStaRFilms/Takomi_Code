using TakomiCode.Domain.Entities;

namespace TakomiCode.Application.Contracts.Persistence;

public interface IWorkspaceRepository
{
    Task<Workspace?> GetWorkspaceAsync(string id, CancellationToken cancellationToken = default);
    Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default);
    Task<IEnumerable<Workspace>> GetAllWorkspacesAsync(CancellationToken cancellationToken = default);
}
