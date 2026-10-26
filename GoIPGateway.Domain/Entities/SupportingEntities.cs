using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class SmsHistory : BaseEntity<long>
{
    public int? SimCardId { get; set; }
    public virtual SimCard? SimCard { get; set; }

    public int? DevicePortId { get; set; }
    public virtual DevicePort? DevicePort { get; set; }

    public SmsDirection Direction { get; set; } = SmsDirection.Incoming;
    public string? SenderNumber { get; set; }
    public string? ReceiverNumber { get; set; }
    public string MessageText { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public bool IsProcessed { get; set; } = false;
}

public class SimBalanceHistory : BaseEntity<long>
{
    public int SimCardId { get; set; }
    public virtual SimCard SimCard { get; set; } = null!;

    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal ChangeAmount { get; set; }
    public BalanceActionType ChangeType { get; set; } = BalanceActionType.None;

    public long? TransactionId { get; set; }
    public virtual TenantTransaction? Transaction { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TenantBalanceMovement : BaseEntity<long>
{
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    public MovementType MovementType { get; set; } = MovementType.ServiceCharge;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    public long? TransactionId { get; set; }
    public virtual TenantTransaction? Transaction { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PricingPlan : BaseEntity
{
    public string PlanName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal MonthlyFee { get; set; } = 0;
    public int MaxDevices { get; set; } = 1;
    public int MaxDailyOps { get; set; } = 1000;
    public int MaxMonthlyOps { get; set; } = 30000;
    public decimal CostPerOperation { get; set; } = 0.10m;

    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
}

public class AuditLog : BaseEntity<long>
{
    public int? TenantId { get; set; }
    public virtual Tenant? Tenant { get; set; }

    public string? UserId { get; set; }
    public string Action { get; set; } = string.Empty; // e.g. "ServiceExecuted", "DeviceAdded"
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }

    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
