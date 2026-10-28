using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Queries;

public record GetTenantApiKeysQuery(int TenantId) : IRequest<Result<IReadOnlyList<TenantApiKey>>>;

public class GetTenantApiKeysQueryHandler : IRequestHandler<GetTenantApiKeysQuery, Result<IReadOnlyList<TenantApiKey>>>
{
    private readonly ITenantQueryService _queryService;

    public GetTenantApiKeysQueryHandler(ITenantQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<IReadOnlyList<TenantApiKey>>> Handle(GetTenantApiKeysQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetApiKeysAsync(request.TenantId, cancellationToken);
    }
}
