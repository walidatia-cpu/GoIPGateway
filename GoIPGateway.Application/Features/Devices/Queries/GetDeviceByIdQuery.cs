using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Queries;

public record GetDeviceByIdQuery(int Id) : IRequest<Result<Device>>;

public class GetDeviceByIdQueryHandler : IRequestHandler<GetDeviceByIdQuery, Result<Device>>
{
    private readonly IDeviceQueryService _queryService;

    public GetDeviceByIdQueryHandler(IDeviceQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<Device>> Handle(GetDeviceByIdQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetWithPortsAndSimsAsync(request.Id, cancellationToken);
    }
}
