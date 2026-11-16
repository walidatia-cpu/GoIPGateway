using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Identity;
using MediatR;

namespace GoIPGateway.Application.Features.Users.Commands;

public record CreateUserCommand(CreateUserDto Dto) : IRequest<Result<CreateUserResultDto>>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResultDto>>
{
    private readonly IIdentityService _identityService;

    public CreateUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<CreateUserResultDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.CreateUserAsync(request.Dto, cancellationToken);
    }
}

public record UpdateUserCommand(UpdateUserDto Dto, int? TenantId = null) : IRequest<Result<bool>>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<bool>>
{
    private readonly IIdentityService _identityService;

    public UpdateUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<bool>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.UpdateUserAsync(request.Dto, request.TenantId, cancellationToken);
    }
}

public record SetUserTempPasswordCommand(string UserId, string? CustomPassword = null, int? TenantId = null) : IRequest<Result<SetTempPasswordResultDto>>;

public class SetUserTempPasswordCommandHandler : IRequestHandler<SetUserTempPasswordCommand, Result<SetTempPasswordResultDto>>
{
    private readonly IIdentityService _identityService;

    public SetUserTempPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<SetTempPasswordResultDto>> Handle(SetUserTempPasswordCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.SetTempPasswordAsync(request.UserId, request.CustomPassword, request.TenantId, cancellationToken);
    }
}

public record ToggleUserStatusCommand(string UserId, int? TenantId = null) : IRequest<Result<bool>>;

public class ToggleUserStatusCommandHandler : IRequestHandler<ToggleUserStatusCommand, Result<bool>>
{
    private readonly IIdentityService _identityService;

    public ToggleUserStatusCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<bool>> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.ToggleUserStatusAsync(request.UserId, request.TenantId, cancellationToken);
    }
}

public record UpdateUserPermissionsCommand(string UserId, List<string> Permissions) : IRequest<Result<bool>>;

public class UpdateUserPermissionsCommandHandler : IRequestHandler<UpdateUserPermissionsCommand, Result<bool>>
{
    private readonly IIdentityService _identityService;

    public UpdateUserPermissionsCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<bool>> Handle(UpdateUserPermissionsCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.UpdateUserPermissionsAsync(request.UserId, request.Permissions, cancellationToken);
    }
}

public record ChangeRequiredPasswordCommand(string UserId, string CurrentPassword, string NewPassword) : IRequest<Result<bool>>;

public class ChangeRequiredPasswordCommandHandler : IRequestHandler<ChangeRequiredPasswordCommand, Result<bool>>
{
    private readonly IIdentityService _identityService;

    public ChangeRequiredPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<bool>> Handle(ChangeRequiredPasswordCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.ChangeRequiredPasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
    }
}

