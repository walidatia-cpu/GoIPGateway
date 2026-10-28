using System.Security.Claims;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Web.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork, ITenantProvider tenantProvider)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            // Check for API Key headers
            if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader) &&
                context.Request.Headers.TryGetValue("X-Secret-Key", out var secretKeyHeader))
            {
                var apiKeyStr = apiKeyHeader.ToString();
                var secretKeyStr = secretKeyHeader.ToString();

                var apiKey = await unitOfWork.TenantApiKeys.AsQueryable()
                    .Include(k => k.Tenant)
                    .FirstOrDefaultAsync(k => k.ApiKey == apiKeyStr && k.SecretKey == secretKeyStr && k.IsActive);

                if (apiKey != null && apiKey.Tenant.IsActive && !apiKey.Tenant.IsBlocked)
                {
                    apiKey.LastUsedAt = DateTime.UtcNow;
                    await unitOfWork.SaveChangesAsync();

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, apiKey.TenantId.ToString()),
                        new Claim(ClaimTypes.Name, apiKey.Tenant.CompanyName),
                        new Claim(ClaimTypes.Role, "TenantApi"),
                        new Claim("TenantId", apiKey.TenantId.ToString()),
                        new Claim("ApiKeyId", apiKey.Id.ToString())
                    };

                    if (!string.IsNullOrEmpty(apiKey.AllowedServices))
                    {
                        claims.Add(new Claim("AllowedServices", apiKey.AllowedServices));
                    }

                    var identity = new ClaimsIdentity(claims, "ApiKey");
                    context.User = new ClaimsPrincipal(identity);
                    tenantProvider.SetCurrentTenantId(apiKey.TenantId);
                }
            }
        }

        await _next(context);
    }
}
