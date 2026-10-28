using GoIPGateway.Domain.Common;

namespace GoIPGateway.Domain.Entities;

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
