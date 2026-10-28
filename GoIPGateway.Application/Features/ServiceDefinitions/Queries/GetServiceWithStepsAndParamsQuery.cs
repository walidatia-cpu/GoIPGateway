using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Queries;

public record GetServiceWithStepsAndParamsQuery(int Id) : IRequest<Result<ServiceDefinition>>;

public class GetServiceWithStepsAndParamsQueryHandler : IRequestHandler<GetServiceWithStepsAndParamsQuery, Result<ServiceDefinition>>
{
    private readonly IServiceDefinitionQueryService _queryService;

    public GetServiceWithStepsAndParamsQueryHandler(IServiceDefinitionQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<ServiceDefinition>> Handle(GetServiceWithStepsAndParamsQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetWithStepsAndParametersAsync(request.Id, cancellationToken);
    }
}
