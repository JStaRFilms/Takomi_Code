using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Application.Contracts.Services;

public interface IGitService
{
    Task<GitState> GetCurrentStateAsync(string workspacePath, CancellationToken cancellationToken = default);
    Task<string> CreateWorktreeAsync(string workspacePath, string worktreeName, string branchName, CancellationToken cancellationToken = default);
    Task EnsureWorktreeIsValidAsync(string workspacePath, CancellationToken cancellationToken = default);
}
