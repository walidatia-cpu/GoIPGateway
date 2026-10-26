using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class Device : BaseEntity, ISoftDelete, IAuditable
{
    public string DeviceName { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; } = DeviceType.GoIP128;
    public string? SerialNumber { get; set; }

    // Network / Auth details
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public int HttpPort { get; set; } = 80;
    public int TelnetPort { get; set; } = 23;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin"; // encrypted

    // Health
    public bool IsOnline { get; set; } = false;
    public DateTime? LastHeartbeat { get; set; }
    public int TotalPorts { get; set; } = 128;
    public int ActivePorts { get; set; } = 0;
    public string? FirmwareVersion { get; set; }

    public string? Notes { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<DevicePort> Ports { get; set; } = new List<DevicePort>();
    public virtual ICollection<TenantDevice> AssignedTenants { get; set; } = new List<TenantDevice>();
}

public class TenantDevice : BaseEntity
{
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    public int DeviceId { get; set; }
    public virtual Device Device { get; set; } = null!;

    public int? PortRangeStart { get; set; }
    public int? PortRangeEnd { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public string? AssignedById { get; set; }
}

public class DevicePort : BaseEntity
{
    public int DeviceId { get; set; }
    public virtual Device Device { get; set; } = null!;

    public int PortNumber { get; set; } // 1-128 or 1-512
    public string PortLabel { get; set; } = string.Empty; // e.g. "L001"

    public PortStatus Status { get; set; } = PortStatus.Empty;
    public bool IsBusy { get; set; } = false;
    public DateTime? BusySince { get; set; }
    public long? CurrentOperationId { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual SimCard? SimCard { get; set; }
    public virtual ICollection<TenantTransaction> Transactions { get; set; } = new List<TenantTransaction>();
}

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
