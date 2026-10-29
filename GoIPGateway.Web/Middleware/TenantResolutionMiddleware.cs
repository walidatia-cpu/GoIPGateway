using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Web.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        ITenantProvider tenantProvider, 
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        SignInManager<ApplicationUser> signInManager)
    {
        if (currentUserService.TenantId.HasValue)
        {
            var tenantId = currentUserService.TenantId.Value;
            tenantProvider.SetCurrentTenantId(tenantId);

            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/Company", StringComparison.OrdinalIgnoreCase))
            {
                var tenant = await unitOfWork.Tenants.AsQueryable()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenant == null || tenant.IsDelete || !tenant.IsActive || tenant.IsBlocked)
                {
                    await signInManager.SignOutAsync();
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }
        }
        else if (currentUserService.IsSuperAdmin || currentUserService.IsAuthenticated)
        {
            // Resolve active tenant for SuperAdmin or global administrators
            int? targetTenantId = null;

            if (context.Request.Cookies.TryGetValue("ActiveTenantId", out var cookieTid) && int.TryParse(cookieTid, out var cTid))
            {
                targetTenantId = cTid;
            }
            else if (context.Request.Query.TryGetValue("tenantId", out var qTid) && int.TryParse(qTid, out var queryTenantId))
            {
                targetTenantId = queryTenantId;
            }
            else if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var hTid) && int.TryParse(hTid, out var headerTenantId))
            {
                targetTenantId = headerTenantId;
            }

            if (!targetTenantId.HasValue)
            {
                var firstTenant = await unitOfWork.Tenants.AsQueryable()
                    .Where(t => t.IsActive && !t.IsDelete)
                    .OrderBy(t => t.Id)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync();

                if (firstTenant > 0)
                {
                    targetTenantId = firstTenant;
                }
            }

            if (targetTenantId.HasValue)
            {
                tenantProvider.SetCurrentTenantId(targetTenantId.Value);
            }
        }

        await _next(context);
    }
}
