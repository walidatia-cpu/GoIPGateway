using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Commands;

public record SwitchDeviceActiveGroupCommand(int DeviceId, string Group) : IRequest<Result<bool>>;

public class SwitchDeviceActiveGroupCommandHandler : IRequestHandler<SwitchDeviceActiveGroupCommand, Result<bool>>
{
    private readonly IDeviceCommandService _deviceCommandService;

    public SwitchDeviceActiveGroupCommandHandler(IDeviceCommandService deviceCommandService)
    {
        _deviceCommandService = deviceCommandService;
    }

    public async Task<Result<bool>> Handle(SwitchDeviceActiveGroupCommand request, CancellationToken cancellationToken)
    {
        return await _deviceCommandService.SwitchActiveGroupAsync(request.DeviceId, request.Group, cancellationToken);
    }
}

