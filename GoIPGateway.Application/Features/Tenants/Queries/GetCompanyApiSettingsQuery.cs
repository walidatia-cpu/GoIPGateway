using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Queries;

public record GetCompanyApiSettingsQuery(int? TenantId = null) : IRequest<Result<Tenant>>;

public class GetCompanyApiSettingsQueryHandler : IRequestHandler<GetCompanyApiSettingsQuery, Result<Tenant>>
{
    private readonly ITenantQueryService _queryService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetCompanyApiSettingsQueryHandler(
        ITenantQueryService queryService,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider)
    {
        _queryService = queryService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<Tenant>> Handle(GetCompanyApiSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<Tenant>("Tenant.NotResolved", "تعذر تحديد حساب الشركة.");

        return await _queryService.GetTenantForApiSettingsAsync(tenantId.Value, cancellationToken);
    }
}
