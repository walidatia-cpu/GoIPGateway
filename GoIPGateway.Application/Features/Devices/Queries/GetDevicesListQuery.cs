using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Queries;

public record GetDevicesListQuery : IRequest<Result<IReadOnlyList<Device>>>;

public class GetDevicesListQueryHandler : IRequestHandler<GetDevicesListQuery, Result<IReadOnlyList<Device>>>
{
    private readonly IDeviceQueryService _queryService;

    public GetDevicesListQueryHandler(IDeviceQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<IReadOnlyList<Device>>> Handle(GetDevicesListQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetAllWithPortsAsync(cancellationToken);
    }
}
