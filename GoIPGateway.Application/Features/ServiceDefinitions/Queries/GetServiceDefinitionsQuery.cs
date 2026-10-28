using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Queries;

public record GetServiceDefinitionsQuery : IRequest<Result<IReadOnlyList<ServiceDefinition>>>;

public class GetServiceDefinitionsQueryHandler : IRequestHandler<GetServiceDefinitionsQuery, Result<IReadOnlyList<ServiceDefinition>>>
{
    private readonly IServiceDefinitionQueryService _queryService;

    public GetServiceDefinitionsQueryHandler(IServiceDefinitionQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<IReadOnlyList<ServiceDefinition>>> Handle(GetServiceDefinitionsQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetAllWithStepsAndParametersAsync(cancellationToken);
    }
}
