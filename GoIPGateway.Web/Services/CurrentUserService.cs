using System.Security.Claims;
using GoIPGateway.Application.Common.Interfaces;

namespace GoIPGateway.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name) ?? User?.FindFirstValue(ClaimTypes.Email);

    public int? TenantId
    {
        get
        {
            var val = User?.FindFirstValue("TenantId");
            return int.TryParse(val, out var tid) ? tid : null;
        }
    }

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsSuperAdmin => User?.IsInRole("SuperAdmin") ?? false;

    public string? AllowedServices => User?.FindFirstValue("AllowedServices");
}

public class TenantProvider : ITenantProvider
{
    private int? _currentTenantId;

    public int? GetCurrentTenantId() => _currentTenantId;

    public void SetCurrentTenantId(int tenantId)
    {
        _currentTenantId = tenantId;
    }

    public bool IsMultiTenant => true;
}
