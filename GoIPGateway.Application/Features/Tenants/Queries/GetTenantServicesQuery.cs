using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Queries;

public record GetTenantServicesQuery(int? TenantId = null) : IRequest<Result<IReadOnlyList<TenantService>>>;

public class GetTenantServicesQueryHandler : IRequestHandler<GetTenantServicesQuery, Result<IReadOnlyList<TenantService>>>
{
    private readonly ITenantQueryService _tenantQueryService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetTenantServicesQueryHandler(
        ITenantQueryService tenantQueryService,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider)
    {
        _tenantQueryService = tenantQueryService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyList<TenantService>>> Handle(GetTenantServicesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<IReadOnlyList<TenantService>>("Tenant.NotResolved", "تعذر تحديد حساب الشركة.");

        return await _tenantQueryService.GetTenantServicesWithDefinitionsAsync(tenantId.Value, cancellationToken);
    }
}
