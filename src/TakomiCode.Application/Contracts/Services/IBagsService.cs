using TakomiCode.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace TakomiCode.Application.Contracts.Services;

public interface IBagsService
{
    Task LinkTokenToWorkspaceAsync(string workspaceId, string tokenAddress, CancellationToken cancellationToken = default);
    Task<bool> CheckVerificationReadinessAsync(string workspaceId, CancellationToken cancellationToken = default);
}
