using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.SimCards;
using MediatR;

namespace GoIPGateway.Application.Features.SimCards.Commands;

public record DeleteSimCardCommand(int Id) : IRequest<Result<bool>>;

public class DeleteSimCardCommandHandler : IRequestHandler<DeleteSimCardCommand, Result<bool>>
{
    private readonly ISimCardCommandService _commandService;

    public DeleteSimCardCommandHandler(ISimCardCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(DeleteSimCardCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.DeleteSimCardAsync(request.Id, cancellationToken);
    }
}
