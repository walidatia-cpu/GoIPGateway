using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Queries;

public record GetTenantByIdQuery(int Id) : IRequest<Result<Tenant>>;

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, Result<Tenant>>
{
    private readonly ITenantQueryService _queryService;

    public GetTenantByIdQueryHandler(ITenantQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<Tenant>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetByIdAsync(request.Id, cancellationToken);
    }
}
