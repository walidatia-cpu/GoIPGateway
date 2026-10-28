using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Commands;

public record ToggleServiceStatusCommand(int Id) : IRequest<Result<bool>>;

public class ToggleServiceStatusCommandValidator : AbstractValidator<ToggleServiceStatusCommand>
{
    public ToggleServiceStatusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class ToggleServiceStatusCommandHandler : IRequestHandler<ToggleServiceStatusCommand, Result<bool>>
{
    private readonly IServiceDefinitionCommandService _commandService;

    public ToggleServiceStatusCommandHandler(IServiceDefinitionCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(ToggleServiceStatusCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.ToggleServiceStatusAsync(request.Id, cancellationToken);
    }
}
