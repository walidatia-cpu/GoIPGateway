using Microsoft.AspNetCore.Authorization;

namespace GoIPGateway.Infrastructure.Identity.Permissions;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

