namespace GoIPGateway.Application.Common.Interfaces;

public interface ITenantProvider
{
    int? GetCurrentTenantId();
    void SetCurrentTenantId(int tenantId);
    bool IsMultiTenant { get; }
}
