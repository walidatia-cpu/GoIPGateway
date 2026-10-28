using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.SimCards;
using MediatR;

namespace GoIPGateway.Application.Features.SimCards.Commands;

public record UpdateSimLimitsCommand(
    int SimId,
    decimal DailyLimit,
    decimal MonthlyLimit
) : IRequest<Result<bool>>;

public class UpdateSimLimitsCommandValidator : AbstractValidator<UpdateSimLimitsCommand>
{
    public UpdateSimLimitsCommandValidator()
    {
        RuleFor(x => x.SimId).GreaterThan(0);
        RuleFor(x => x.DailyLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MonthlyLimit).GreaterThanOrEqualTo(0);
    }
}

public class UpdateSimLimitsCommandHandler : IRequestHandler<UpdateSimLimitsCommand, Result<bool>>
{
    private readonly ISimCardCommandService _commandService;

    public UpdateSimLimitsCommandHandler(ISimCardCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(UpdateSimLimitsCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.UpdateLimitsAsync(request.SimId, request.DailyLimit, request.MonthlyLimit, cancellationToken);
    }
}
