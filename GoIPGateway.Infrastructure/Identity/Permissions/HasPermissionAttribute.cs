using Microsoft.AspNetCore.Authorization;

namespace GoIPGateway.Infrastructure.Identity.Permissions;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(permission)
    {
    }
}

