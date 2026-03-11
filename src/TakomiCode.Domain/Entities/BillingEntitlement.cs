namespace TakomiCode.Domain.Entities;

public class BillingEntitlement
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Provider { get; set; } = "Paystack";
    public string Email { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime ActivatedAt { get; set; }
}
