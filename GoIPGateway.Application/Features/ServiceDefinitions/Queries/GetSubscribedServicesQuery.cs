using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Queries;

public record GetSubscribedServicesQuery(int? TenantId = null) : IRequest<Result<IReadOnlyList<ServiceDefinition>>>;

public class GetSubscribedServicesQueryHandler : IRequestHandler<GetSubscribedServicesQuery, Result<IReadOnlyList<ServiceDefinition>>>
{
    private readonly IServiceDefinitionQueryService _queryService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetSubscribedServicesQueryHandler(
        IServiceDefinitionQueryService queryService,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider)
    {
        _queryService = queryService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyList<ServiceDefinition>>> Handle(GetSubscribedServicesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<IReadOnlyList<ServiceDefinition>>("Tenant.NotResolved", "تعذر تحديد حساب الشركة.");

        return await _queryService.GetSubscribedByTenantAsync(tenantId.Value, cancellationToken);
    }
}
