using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Commands;

public record DeleteServiceStepCommand(int StepId, int ServiceId) : IRequest<Result<bool>>;

public class DeleteServiceStepCommandHandler : IRequestHandler<DeleteServiceStepCommand, Result<bool>>
{
    private readonly IServiceDefinitionCommandService _commandService;

    public DeleteServiceStepCommandHandler(IServiceDefinitionCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(DeleteServiceStepCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.DeleteStepAsync(request.StepId, request.ServiceId, cancellationToken);
    }
}
