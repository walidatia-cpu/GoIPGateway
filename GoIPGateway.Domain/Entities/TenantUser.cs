using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class TenantUser : BaseEntity<string>
{
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    public TenantRole Role { get; set; } = TenantRole.TenantOperator;
    public bool IsActive { get; set; } = true;
}
