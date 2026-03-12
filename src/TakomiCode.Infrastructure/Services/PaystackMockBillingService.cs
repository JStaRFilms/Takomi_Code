using System.Text.Json;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Application.Contracts.Services;
using TakomiCode.Domain.Entities;
using TakomiCode.Domain.Events;

namespace TakomiCode.Infrastructure.Services;

/// <summary>
/// Development-only billing service that simulates Paystack checkout and entitlement activation.
/// Billing state is persisted to a local JSON file and no real payment provider is contacted.
/// </summary>
public class PaystackMockBillingService : IBillingService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;
    private BillingStore? _store;

    public PaystackMockBillingService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;

        var baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TakomiCode");

        Directory.CreateDirectory(baseDirectory);
        _storePath = Path.Combine(baseDirectory, "billing-state.json");
    }

    public async Task<string> CreateCheckoutSessionAsync(string projectId, string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException("Project id is required for checkout.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Customer email is required for checkout.");
        }

        await EnsureLoadedAsync(cancellationToken);

        var referenceId = $"paystack_{Guid.NewGuid():N}";
        var checkoutUrl = $"https://checkout.paystack.com/{referenceId}";
        _store!.PendingCheckouts[projectId] = new BillingCheckoutSession
        {
            ProjectId = projectId,
            Email = email,
            ReferenceId = referenceId,
            CheckoutUrl = checkoutUrl
        };

        await PersistAsync(cancellationToken);
        await AppendAuditEventAsync(
            "billing.checkout_started",
            projectId,
            $"Started mock Paystack checkout for project '{projectId}' with reference '{referenceId}'.",
            cancellationToken);

        return checkoutUrl;
    }

    public async Task<BillingEntitlement> ActivateEntitlementAsync(string projectId, string referenceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException("Project id is required for entitlement activation.");
        }

        if (string.IsNullOrWhiteSpace(referenceId))
        {
            throw new InvalidOperationException("Paystack reference id is required for entitlement activation.");
        }

        await EnsureLoadedAsync(cancellationToken);

        if (!_store!.PendingCheckouts.TryGetValue(projectId, out var checkout))
        {
            throw new InvalidOperationException($"No pending Paystack checkout exists for project '{projectId}'.");
        }

        if (!string.Equals(checkout.ReferenceId, referenceId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Paystack reference '{referenceId}' does not match the pending checkout for project '{projectId}'.");
        }

        var entitlement = new BillingEntitlement
        {
            Id = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            Provider = checkout.Provider,
            Email = checkout.Email,
            ReferenceId = checkout.ReferenceId,
            AccessLevel = "Pro",
            IsActive = true,
            ActivatedAt = DateTime.UtcNow
        };

        checkout.IsCompleted = true;
        _store.Entitlements[projectId] = entitlement;
        _store.PendingCheckouts.Remove(projectId);

        await PersistAsync(cancellationToken);
        await AppendAuditEventAsync(
            "billing.entitlement_activated",
            projectId,
            $"Activated mock Pro entitlement for project '{projectId}' using Paystack reference '{referenceId}'.",
            cancellationToken);

        return Clone(entitlement)!;
    }

    public async Task<BillingEntitlement?> GetEntitlementAsync(string projectId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        _store!.Entitlements.TryGetValue(projectId, out var entitlement);
        return Clone(entitlement);
    }

    public async Task<BillingCheckoutSession?> GetPendingCheckoutAsync(string projectId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        _store!.PendingCheckouts.TryGetValue(projectId, out var checkout);
        return Clone(checkout);
    }

    private async Task AppendAuditEventAsync(string eventType, string projectId, string description, CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent
        {
            WorkspaceId = projectId,
            EventType = eventType,
            Description = description
        };

        await _auditLogRepository.AppendEventAsync(auditEvent, cancellationToken);
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_store is not null)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_store is not null)
            {
                return;
            }

            if (!File.Exists(_storePath))
            {
                _store = new BillingStore();
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_storePath, cancellationToken);
                _store = JsonSerializer.Deserialize<BillingStore>(json, SerializerOptions) ?? new BillingStore();
            }
            catch (JsonException)
            {
                var corruptStorePath = Path.Combine(
                    Path.GetDirectoryName(_storePath)!,
                    $"billing-state.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");

                File.Move(_storePath, corruptStorePath, overwrite: true);
                _store = new BillingStore();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(_store, SerializerOptions);
            await File.WriteAllTextAsync(_storePath, json, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static T? Clone<T>(T? value)
    {
        if (value is null)
        {
            return default;
        }

        var json = JsonSerializer.Serialize(value, SerializerOptions);
        return JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    private sealed class BillingStore
    {
        public Dictionary<string, BillingEntitlement> Entitlements { get; set; } = new();
        public Dictionary<string, BillingCheckoutSession> PendingCheckouts { get; set; } = new();
    }
}
