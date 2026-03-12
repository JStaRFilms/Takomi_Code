using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Application.Contracts.Persistence;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetSessionAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetSessionsByWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetChildSessionsAsync(string parentSessionId, CancellationToken cancellationToken = default);
    Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string id, CancellationToken cancellationToken = default);
}
