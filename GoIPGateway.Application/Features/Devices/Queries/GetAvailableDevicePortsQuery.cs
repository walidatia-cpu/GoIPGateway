using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Queries;

public record GetAvailableDevicePortsQuery() : IRequest<Result<IReadOnlyList<DevicePort>>>;

public class GetAvailableDevicePortsQueryHandler : IRequestHandler<GetAvailableDevicePortsQuery, Result<IReadOnlyList<DevicePort>>>
{
    private readonly IDeviceQueryService _deviceQueryService;

    public GetAvailableDevicePortsQueryHandler(IDeviceQueryService deviceQueryService)
    {
        _deviceQueryService = deviceQueryService;
    }

    public async Task<Result<IReadOnlyList<DevicePort>>> Handle(GetAvailableDevicePortsQuery request, CancellationToken cancellationToken)
    {
        return await _deviceQueryService.GetAvailablePortsAsync(cancellationToken);
    }
}
