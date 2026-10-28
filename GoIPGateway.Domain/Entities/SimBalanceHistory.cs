using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

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
