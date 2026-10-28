using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.SimCards;
using MediatR;

namespace GoIPGateway.Application.Features.SimCards.Commands;

public record ToggleSimCardStatusCommand(int Id) : IRequest<Result<bool>>;

public class ToggleSimCardStatusCommandValidator : AbstractValidator<ToggleSimCardStatusCommand>
{
    public ToggleSimCardStatusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class ToggleSimCardStatusCommandHandler : IRequestHandler<ToggleSimCardStatusCommand, Result<bool>>
{
    private readonly ISimCardCommandService _commandService;

    public ToggleSimCardStatusCommandHandler(ISimCardCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(ToggleSimCardStatusCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.ToggleSimCardStatusAsync(request.Id, cancellationToken);
    }
}
