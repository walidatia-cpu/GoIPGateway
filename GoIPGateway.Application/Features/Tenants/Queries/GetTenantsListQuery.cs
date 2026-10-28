using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Queries;

public record GetTenantsListQuery : IRequest<Result<IReadOnlyList<Tenant>>>;

public class GetTenantsListQueryHandler : IRequestHandler<GetTenantsListQuery, Result<IReadOnlyList<Tenant>>>
{
    private readonly ITenantQueryService _queryService;

    public GetTenantsListQueryHandler(ITenantQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<IReadOnlyList<Tenant>>> Handle(GetTenantsListQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetActiveTenantsWithStatsAsync(cancellationToken);
    }
}
