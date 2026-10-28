using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Commands;

public record DeleteServiceDefinitionCommand(int Id) : IRequest<Result<bool>>;

public class DeleteServiceDefinitionCommandHandler : IRequestHandler<DeleteServiceDefinitionCommand, Result<bool>>
{
    private readonly IServiceDefinitionCommandService _commandService;

    public DeleteServiceDefinitionCommandHandler(IServiceDefinitionCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(DeleteServiceDefinitionCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.DeleteServiceAsync(request.Id, cancellationToken);
    }
}
