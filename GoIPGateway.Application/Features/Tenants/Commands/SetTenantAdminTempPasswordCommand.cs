using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Identity;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record SetTenantAdminTempPasswordCommand(int TenantId, string? CustomPassword = null) : IRequest<Result<SetTempPasswordResultDto>>;

public class SetTenantAdminTempPasswordCommandHandler : IRequestHandler<SetTenantAdminTempPasswordCommand, Result<SetTempPasswordResultDto>>
{
    private readonly IIdentityService _identityService;

    public SetTenantAdminTempPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<SetTempPasswordResultDto>> Handle(SetTenantAdminTempPasswordCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.SetTenantAdminTempPasswordAsync(request.TenantId, request.CustomPassword, cancellationToken);
    }
}

