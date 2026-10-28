using GoIPGateway.Application.Common.Interfaces;

namespace GoIPGateway.Web.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider, ICurrentUserService currentUserService)
    {
        if (currentUserService.TenantId.HasValue)
        {
            tenantProvider.SetCurrentTenantId(currentUserService.TenantId.Value);
        }

        await _next(context);
    }
}
