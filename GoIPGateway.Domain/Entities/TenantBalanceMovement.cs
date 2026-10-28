using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

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
