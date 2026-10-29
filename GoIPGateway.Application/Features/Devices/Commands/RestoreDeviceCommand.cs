using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Commands;

public record RestoreDeviceCommand(int Id) : IRequest<Result<bool>>;

public class RestoreDeviceCommandHandler : IRequestHandler<RestoreDeviceCommand, Result<bool>>
{
    private readonly IDeviceCommandService _commandService;

    public RestoreDeviceCommandHandler(IDeviceCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(RestoreDeviceCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.RestoreDeviceAsync(request.Id, cancellationToken);
    }
}
