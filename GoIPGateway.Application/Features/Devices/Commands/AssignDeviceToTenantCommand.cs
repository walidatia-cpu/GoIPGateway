using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Commands;

public record AssignDeviceToTenantCommand(
    int DeviceId,
    int TenantId,
    int? PortRangeStart,
    int? PortRangeEnd
) : IRequest<Result<bool>>;

public class AssignDeviceToTenantCommandValidator : AbstractValidator<AssignDeviceToTenantCommand>
{
    public AssignDeviceToTenantCommandValidator()
    {
        RuleFor(x => x.DeviceId).GreaterThan(0);
        RuleFor(x => x.TenantId).GreaterThan(0);
    }
}

public class AssignDeviceToTenantCommandHandler : IRequestHandler<AssignDeviceToTenantCommand, Result<bool>>
{
    private readonly IDeviceCommandService _commandService;

    public AssignDeviceToTenantCommandHandler(IDeviceCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(AssignDeviceToTenantCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.AssignDeviceToTenantAsync(
            request.DeviceId,
            request.TenantId,
            request.PortRangeStart,
            request.PortRangeEnd,
            cancellationToken);
    }
}
