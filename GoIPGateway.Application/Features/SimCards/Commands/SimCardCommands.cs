using FluentValidation;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        RuleFor(x => x.DevicePortId).GreaterThan(0);
        RuleFor(x => x.DailyLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MonthlyLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
    }
}

public class RegisterSimCardCommandHandler : IRequestHandler<RegisterSimCardCommand, Result<int>>
{
    private readonly IAppDbContext _context;

    public RegisterSimCardCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<int>> Handle(RegisterSimCardCommand request, CancellationToken cancellationToken)
    {
        var port = await _context.DevicePorts
            .Include(p => p.SimCard)
            .FirstOrDefaultAsync(p => p.Id == request.DevicePortId && p.IsActive, cancellationToken);

        if (port == null)
            return Result.Failure<int>("Port.NotFound", "بورت الجهاز غير موجود.");

        if (port.SimCard != null && !port.SimCard.IsDelete)
            return Result.Failure<int>("Port.Occupied", $"البورت {port.PortLabel} يحتوي بالفعل على شريحة مفعلة.");

        var sim = new SimCard
        {
            DevicePortId = port.Id,
            Operator = request.Operator,
            PhoneNumber = request.PhoneNumber,
            SimNumber = request.SimNumber,
            ICCID = request.ICCID,
            WalletPassword = request.WalletPassword,
            UssdPassword = request.UssdPassword,
            DailyLimit = request.DailyLimit,
            MonthlyLimit = request.MonthlyLimit,
            CurrentBalance = request.InitialBalance,
            PreviousBalance = request.InitialBalance,
            Notes = request.Notes,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        port.Status = PortStatus.Active;

        _context.SimCards.Add(sim);
        await _context.SaveChangesAsync(cancellationToken);

        // Update Device active ports count
        var activeCount = await _context.SimCards
            .CountAsync(s => s.DevicePort.DeviceId == port.DeviceId && s.IsActive && !s.IsDelete, cancellationToken);
        var device = await _context.Devices.FindAsync(new object[] { port.DeviceId }, cancellationToken);
        if (device != null)
        {
            device.ActivePorts = activeCount;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(sim.Id);
    }
}
