using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Queries;

public record GetServiceStepByIdQuery(int StepId) : IRequest<Result<ServiceUssdStep>>;

public class GetServiceStepByIdQueryHandler : IRequestHandler<GetServiceStepByIdQuery, Result<ServiceUssdStep>>
{
    private readonly IServiceDefinitionQueryService _queryService;

    public GetServiceStepByIdQueryHandler(IServiceDefinitionQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<ServiceUssdStep>> Handle(GetServiceStepByIdQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetStepByIdAsync(request.StepId, cancellationToken);
    }
}
