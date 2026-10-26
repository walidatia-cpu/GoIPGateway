using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

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

public class TenantApiKey : BaseEntity
{
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string? KeyName { get; set; }

    public string? AllowedIPs { get; set; } // JSON array of IPs or CIDRs
    public string? AllowedServices { get; set; } // JSON array of allowed ServiceDefinition IDs (null = all)
    public int RateLimit { get; set; } = 100; // Requests per minute

    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class TenantUser : BaseEntity<string>
{
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    public TenantRole Role { get; set; } = TenantRole.TenantOperator;
    public bool IsActive { get; set; } = true;
}
