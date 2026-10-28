using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Commands;

public record DeleteDeviceCommand(int Id) : IRequest<Result<bool>>;

public class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand, Result<bool>>
{
    private readonly IDeviceCommandService _commandService;

    public DeleteDeviceCommandHandler(IDeviceCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.DeleteDeviceAsync(request.Id, cancellationToken);
    }
}
