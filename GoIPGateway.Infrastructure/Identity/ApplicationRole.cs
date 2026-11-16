using Microsoft.AspNetCore.Identity;

namespace GoIPGateway.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public int? TenantId { get; set; }
    public bool IsSystemRole { get; set; } = false;

    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
