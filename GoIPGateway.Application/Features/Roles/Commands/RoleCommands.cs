using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Identity;
using MediatR;

namespace GoIPGateway.Application.Features.Roles.Commands;

public record CreateRoleCommand(CreateRoleDto Dto) : IRequest<Result<string>>;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<string>>
{
    private readonly IIdentityService _identityService;

    public CreateRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<string>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.CreateRoleAsync(request.Dto, cancellationToken);
    }
}

public record UpdateRoleCommand(UpdateRoleDto Dto, int? TenantId = null) : IRequest<Result<bool>>;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<bool>>
{
    private readonly IIdentityService _identityService;

    public UpdateRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<bool>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.UpdateRoleAsync(request.Dto, request.TenantId, cancellationToken);
    }
}

public record DeleteRoleCommand(string Id, int? TenantId = null) : IRequest<Result<bool>>;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<bool>>
{
    private readonly IIdentityService _identityService;

    public DeleteRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.DeleteRoleAsync(request.Id, request.TenantId, cancellationToken);
    }
}

