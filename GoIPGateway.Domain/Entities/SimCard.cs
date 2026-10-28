using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class SimCard : BaseEntity, ISoftDelete, IAuditable
{
    public int DevicePortId { get; set; }
    public virtual DevicePort DevicePort { get; set; } = null!;

    public string? SimNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ICCID { get; set; }
    public OperatorType Operator { get; set; } = OperatorType.Vodafone;

    // Balances
    public decimal CurrentBalance { get; set; } = 0;
    public decimal PreviousBalance { get; set; } = 0;
    public DateTime? LastBalanceCheck { get; set; }

    // Security/Passwords
    public string? WalletPassword { get; set; }
    public string? UssdPassword { get; set; }

    // Quotas / Limits
    public decimal DailyLimit { get; set; } = 0;
    public decimal MonthlyLimit { get; set; } = 0;
    public decimal DailyConsumed { get; set; } = 0;
    public decimal MonthlyConsumed { get; set; } = 0;

    // Bank reconciliation
    public int? BankAccountId { get; set; }
    public decimal BankBalance { get; set; } = 0;

    // Status
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; } = false;
    public string? BlockReason { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual ICollection<SimBalanceHistory> BalanceHistories { get; set; } = new List<SimBalanceHistory>();
    public virtual ICollection<SmsHistory> SmsHistories { get; set; } = new List<SmsHistory>();
    public virtual ICollection<TenantTransaction> Transactions { get; set; } = new List<TenantTransaction>();
}
