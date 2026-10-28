using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

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
