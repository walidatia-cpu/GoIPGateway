using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.SimCards;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using MediatR;

namespace GoIPGateway.Application.Features.SimCards.Queries;

public record GetSimCardsListQuery(OperatorType? Operator = null) : IRequest<Result<IReadOnlyList<SimCard>>>;

public class GetSimCardsListQueryHandler : IRequestHandler<GetSimCardsListQuery, Result<IReadOnlyList<SimCard>>>
{
    private readonly ISimCardQueryService _queryService;

    public GetSimCardsListQueryHandler(ISimCardQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<IReadOnlyList<SimCard>>> Handle(GetSimCardsListQuery request, CancellationToken cancellationToken)
    {
        if (request.Operator.HasValue)
        {
            return await _queryService.GetByOperatorAsync(request.Operator.Value, cancellationToken);
        }

        return await _queryService.GetAllWithPortAndDeviceAsync(cancellationToken);
    }
}
