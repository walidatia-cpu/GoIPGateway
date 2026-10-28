using GoIPGateway.Domain.Common;

namespace GoIPGateway.Domain.Entities;

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
