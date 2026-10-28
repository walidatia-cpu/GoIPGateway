using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.SimCards;
using GoIPGateway.Domain.Enums;
using MediatR;

namespace GoIPGateway.Application.Features.SimCards.Commands;

public record RegisterSimCardCommand(
    int DevicePortId,
    OperatorType Operator,
    string? PhoneNumber,
    string? SimNumber,
    string? ICCID,
    string? WalletPassword,
    string? UssdPassword,
    decimal DailyLimit = 0,
    decimal MonthlyLimit = 0,
    decimal InitialBalance = 0,
    string? Notes = null
) : IRequest<Result<int>>;

public class RegisterSimCardCommandValidator : AbstractValidator<RegisterSimCardCommand>
{
    public RegisterSimCardCommandValidator()
    {
        RuleFor(x => x.DevicePortId).GreaterThan(0).WithMessage("معرف البورت مطلوب");
        RuleFor(x => x.DailyLimit).GreaterThanOrEqualTo(0).WithMessage("الحد اليومي غير صالح");
        RuleFor(x => x.MonthlyLimit).GreaterThanOrEqualTo(0).WithMessage("الحد الشهري غير صالح");
        RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0).WithMessage("الرصيد الابتدائي غير صالح");
    }
}

public class RegisterSimCardCommandHandler : IRequestHandler<RegisterSimCardCommand, Result<int>>
{
    private readonly ISimCardCommandService _commandService;

    public RegisterSimCardCommandHandler(ISimCardCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<int>> Handle(RegisterSimCardCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.RegisterSimCardAsync(request, cancellationToken);
    }
}
