using GoIPGateway.Domain.Common;

namespace GoIPGateway.Domain.Entities;

public class Tenant : BaseEntity, ISoftDelete, IAuditable
{
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyNameEn { get; set; }
    public string? LogoUrl { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? TaxId { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; } = false;
    public string? BlockReason { get; set; }

    // Quotas & Limits
    public int MaxDevices { get; set; } = 1;
    public int MaxDailyOperations { get; set; } = 1000;
    public int MaxMonthlyOps { get; set; } = 30000;

    // Financial
    public decimal Balance { get; set; } = 0;
    public decimal CreditLimit { get; set; } = 0;
    public int? PricingPlanId { get; set; }
    public virtual PricingPlan? PricingPlan { get; set; }

    // Contract
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }

    // Navigation
    public virtual ICollection<TenantApiKey> ApiKeys { get; set; } = new List<TenantApiKey>();
    public virtual ICollection<TenantUser> Users { get; set; } = new List<TenantUser>();
    public virtual ICollection<TenantDevice> AssignedDevices { get; set; } = new List<TenantDevice>();
    public virtual ICollection<TenantService> SubscribedServices { get; set; } = new List<TenantService>();
    public virtual ICollection<TenantTransaction> Transactions { get; set; } = new List<TenantTransaction>();
    public virtual ICollection<TenantBalanceMovement> BalanceMovements { get; set; } = new List<TenantBalanceMovement>();
}
