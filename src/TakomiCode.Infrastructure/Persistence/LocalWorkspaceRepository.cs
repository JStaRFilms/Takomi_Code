using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Infrastructure.Persistence;

public class LocalWorkspaceRepository : IWorkspaceRepository
{
    private readonly Dictionary<string, Workspace> _workspaces = new();

    public Task<IEnumerable<Workspace>> GetAllWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_workspaces.Values.AsEnumerable());
    }

    public Task<Workspace?> GetWorkspaceAsync(string id, CancellationToken cancellationToken = default)
    {
        _workspaces.TryGetValue(id, out var workspace);
        return Task.FromResult(workspace);
    }

    public Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        _workspaces[workspace.Id] = workspace;
        return Task.CompletedTask;
    }
}
