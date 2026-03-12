using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Application.Contracts.Services;

public interface IBillingService
{
    Task<string> CreateCheckoutSessionAsync(string projectId, string email, CancellationToken cancellationToken = default);
    Task<BillingEntitlement> ActivateEntitlementAsync(string projectId, string referenceId, CancellationToken cancellationToken = default);
    Task<BillingEntitlement?> GetEntitlementAsync(string projectId, CancellationToken cancellationToken = default);
    Task<BillingCheckoutSession?> GetPendingCheckoutAsync(string projectId, CancellationToken cancellationToken = default);
}
