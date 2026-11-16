using System.Security.Claims;
using GoIPGateway.Application.Common.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GoIPGateway.Infrastructure.Identity;

public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public CustomUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.TenantId.HasValue)
        {
            identity.AddClaim(new Claim("TenantId", user.TenantId.Value.ToString()));
            identity.AddClaim(new Claim("tenant_id", user.TenantId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(user.FullName))
        {
            identity.AddClaim(new Claim("FullName", user.FullName));
        }

        identity.AddClaim(new Claim("MustChangePassword", user.MustChangePassword.ToString()));

        if (user.IsOwner)
        {
            identity.AddClaim(new Claim("IsOwner", "True"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Owner"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "SuperAdmin"));
        }

        // Ensure all role permission claims are explicitly added to the identity
        var userRoles = await UserManager.GetRolesAsync(user);
        foreach (var roleName in userRoles)
        {
            var role = await RoleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var roleClaims = await RoleManager.GetClaimsAsync(role);
                foreach (var rc in roleClaims.Where(c => c.Type == AppPermissions.ClaimType))
                {
                    if (!identity.HasClaim(rc.Type, rc.Value))
                    {
                        identity.AddClaim(new Claim(rc.Type, rc.Value));
                    }
                }
            }
        }

        return identity;
    }
}
