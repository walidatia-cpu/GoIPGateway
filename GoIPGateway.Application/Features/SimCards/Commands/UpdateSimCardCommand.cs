using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.SimCards;
using GoIPGateway.Domain.Enums;
using MediatR;

namespace GoIPGateway.Application.Features.SimCards.Commands;

public record UpdateSimCardCommand(
    int Id,
    string? PhoneNumber,
    string? SimNumber,
    string? ICCID,
    OperatorType Operator,
    string? WalletPassword,
    string? UssdPassword,
    decimal DailyLimit,
    decimal MonthlyLimit,
    bool IsActive,
    bool IsBlocked,
    decimal CurrentBalance,
    string? Notes
) : IRequest<Result<bool>>;

public class UpdateSimCardCommandValidator : AbstractValidator<UpdateSimCardCommand>
{
    public UpdateSimCardCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.DailyLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MonthlyLimit).GreaterThanOrEqualTo(0);
    }
}

public class UpdateSimCardCommandHandler : IRequestHandler<UpdateSimCardCommand, Result<bool>>
{
    private readonly ISimCardCommandService _commandService;

    public UpdateSimCardCommandHandler(ISimCardCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(UpdateSimCardCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.UpdateSimCardAsync(
            request.Id,
            request.PhoneNumber,
            request.SimNumber,
            request.ICCID,
            request.Operator,
            request.WalletPassword,
            request.UssdPassword,
            request.DailyLimit,
            request.MonthlyLimit,
            request.IsActive,
            request.IsBlocked,
            request.CurrentBalance,
            request.Notes,
            cancellationToken);
    }
}
