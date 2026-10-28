using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.SimCards;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.SimCards.Queries;

public record GetCompanySimCardsQuery(int? TenantId = null) : IRequest<Result<IReadOnlyList<SimCard>>>;

public class GetCompanySimCardsQueryHandler : IRequestHandler<GetCompanySimCardsQuery, Result<IReadOnlyList<SimCard>>>
{
    private readonly ISimCardQueryService _queryService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetCompanySimCardsQueryHandler(
        ISimCardQueryService queryService,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider)
    {
        _queryService = queryService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyList<SimCard>>> Handle(GetCompanySimCardsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<IReadOnlyList<SimCard>>("Tenant.NotResolved", "تعذر تحديد حساب الشركة.");

        return await _queryService.GetAssignedToTenantAsync(tenantId.Value, cancellationToken);
    }
}
