using System.Security.Claims;
using GoIPGateway.Application.Common.Security;

namespace GoIPGateway.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool HasPermission(this ClaimsPrincipal? principal, string permission)
    {
        if (principal == null || principal.Identity?.IsAuthenticated != true)
            return false;

        // Owner or SuperAdmin has unrestricted wildcard access to everything
        if (principal.HasClaim("IsOwner", "True") || 
            principal.IsInRole("Owner") || 
            principal.IsInRole("SuperAdmin"))
        {
            return true;
        }

        // Check if the permission claim is present
        return principal.Claims.Any(c =>
            string.Equals(c.Type, AppPermissions.ClaimType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
    }
}

