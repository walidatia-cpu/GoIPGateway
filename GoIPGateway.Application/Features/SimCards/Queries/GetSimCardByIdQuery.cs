using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.SimCards;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.SimCards.Queries;

public record GetSimCardByIdQuery(int Id) : IRequest<Result<SimCard>>;

public class GetSimCardByIdQueryHandler : IRequestHandler<GetSimCardByIdQuery, Result<SimCard>>
{
    private readonly ISimCardQueryService _queryService;

    public GetSimCardByIdQueryHandler(ISimCardQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<SimCard>> Handle(GetSimCardByIdQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetWithPortAndDeviceAsync(request.Id, cancellationToken);
    }
}
