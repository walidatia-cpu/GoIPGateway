using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Commands;

public record SwitchDeviceBlockSlotCommand(int DeviceId, int PortId) : IRequest<Result<bool>>;

public class SwitchDeviceBlockSlotCommandHandler : IRequestHandler<SwitchDeviceBlockSlotCommand, Result<bool>>
{
    private readonly IDeviceCommandService _deviceCommandService;

    public SwitchDeviceBlockSlotCommandHandler(IDeviceCommandService deviceCommandService)
    {
        _deviceCommandService = deviceCommandService;
    }

    public async Task<Result<bool>> Handle(SwitchDeviceBlockSlotCommand request, CancellationToken cancellationToken)
    {
        return await _deviceCommandService.SwitchBlockSlotAsync(request.DeviceId, request.PortId, cancellationToken);
    }
}

