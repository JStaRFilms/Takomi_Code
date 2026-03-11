using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Application.Contracts.Services;

public interface IInterventionCommandHandler
{
    Task ExecuteInterventionAsync(string runId, InterventionAction action, string? payload = null, CancellationToken cancellationToken = default);
}
