using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Queries;

public record GetCompanyEmptyPortsQuery(int? TenantId = null) : IRequest<Result<IReadOnlyList<DevicePort>>>;

public class GetCompanyEmptyPortsQueryHandler : IRequestHandler<GetCompanyEmptyPortsQuery, Result<IReadOnlyList<DevicePort>>>
{
    private readonly IDeviceQueryService _deviceQueryService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetCompanyEmptyPortsQueryHandler(
        IDeviceQueryService deviceQueryService,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider)
    {
        _deviceQueryService = deviceQueryService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyList<DevicePort>>> Handle(GetCompanyEmptyPortsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<IReadOnlyList<DevicePort>>("Tenant.NotResolved", "تعذر تحديد حساب الشركة.");

        return await _deviceQueryService.GetCompanyEmptyPortsAsync(tenantId.Value, cancellationToken);
    }
}
