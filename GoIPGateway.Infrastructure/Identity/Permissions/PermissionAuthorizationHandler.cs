using System.Security.Claims;
using GoIPGateway.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;

namespace GoIPGateway.Infrastructure.Identity.Permissions;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User == null)
            return Task.CompletedTask;

        // Owner or SuperAdmin has unrestricted wildcard access to everything in the platform
        if (context.User.HasClaim("IsOwner", "True") || 
            context.User.IsInRole("Owner") || 
            context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if the permission claim exists in the principal (direct or via role claims)
        var hasPermission = context.User.Claims.Any(c =>
            string.Equals(c.Type, AppPermissions.ClaimType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
