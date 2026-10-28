using GoIPGateway.Application.Common.Interfaces;

namespace GoIPGateway.Web.Services;

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
