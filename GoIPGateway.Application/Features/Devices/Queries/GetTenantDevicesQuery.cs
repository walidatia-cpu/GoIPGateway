using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Queries;

public record GetTenantDevicesQuery(int? TenantId = null) : IRequest<Result<IReadOnlyList<Device>>>;

public class GetTenantDevicesQueryHandler : IRequestHandler<GetTenantDevicesQuery, Result<IReadOnlyList<Device>>>
{
    private readonly IDeviceQueryService _queryService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetTenantDevicesQueryHandler(
        IDeviceQueryService queryService,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider)
    {
        _queryService = queryService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyList<Device>>> Handle(GetTenantDevicesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<IReadOnlyList<Device>>("Tenant.NotResolved", "تعذر تحديد حساب الشركة.");

        return await _queryService.GetTenantDevicesAsync(tenantId.Value, cancellationToken);
    }
}
