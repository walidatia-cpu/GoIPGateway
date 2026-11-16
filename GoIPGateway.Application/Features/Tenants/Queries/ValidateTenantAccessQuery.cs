using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Queries;

public record ValidateTenantAccessQuery(int TenantId) : IRequest<Result<bool>>;

public class ValidateTenantAccessQueryHandler : IRequestHandler<ValidateTenantAccessQuery, Result<bool>>
{
    private readonly IIdentityService _identityService;

    public ValidateTenantAccessQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<bool>> Handle(ValidateTenantAccessQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.ValidateTenantAccessAsync(request.TenantId, cancellationToken);
    }
}

