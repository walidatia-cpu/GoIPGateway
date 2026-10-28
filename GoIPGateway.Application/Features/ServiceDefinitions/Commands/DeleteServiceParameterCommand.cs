using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Commands;

public record DeleteServiceParameterCommand(int Id, int ServiceId) : IRequest<Result<bool>>;

public class DeleteServiceParameterCommandHandler : IRequestHandler<DeleteServiceParameterCommand, Result<bool>>
{
    private readonly IServiceDefinitionCommandService _commandService;

    public DeleteServiceParameterCommandHandler(IServiceDefinitionCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(DeleteServiceParameterCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.DeleteParameterAsync(request.Id, request.ServiceId, cancellationToken);
    }
}
