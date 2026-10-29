using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Queries;

public record GetTenantDeviceAssignmentsQuery(int? TenantId = null, bool IncludeDeleted = true) : IRequest<Result<IReadOnlyList<TenantDevice>>>;

public class GetTenantDeviceAssignmentsQueryHandler : IRequestHandler<GetTenantDeviceAssignmentsQuery, Result<IReadOnlyList<TenantDevice>>>
{
    private readonly IDeviceQueryService _queryService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetTenantDeviceAssignmentsQueryHandler(
        IDeviceQueryService queryService,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider)
    {
        _queryService = queryService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyList<TenantDevice>>> Handle(GetTenantDeviceAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<IReadOnlyList<TenantDevice>>("Tenant.NotResolved", "تعذر تحديد حساب الشركة.");

        return await _queryService.GetTenantDeviceAssignmentsAsync(tenantId.Value, request.IncludeDeleted, cancellationToken);
    }
}
