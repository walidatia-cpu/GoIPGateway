using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Features.SimCards.Commands;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Services.SimCards;

public class SimCardCommandService : ISimCardCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISignalRNotifier _notifier;

    public SimCardCommandService(IUnitOfWork unitOfWork, ISignalRNotifier notifier)
    {
        _unitOfWork = unitOfWork;
        _notifier = notifier;
    }

    public async Task<Result<int>> RegisterSimCardAsync(RegisterSimCardCommand command, CancellationToken cancellationToken = default)
    {
        var port = await _unitOfWork.DevicePorts.AsQueryable()
            .Include(p => p.SimCard)
            .FirstOrDefaultAsync(p => p.Id == command.DevicePortId && p.IsActive, cancellationToken);

        if (port == null)
            return Result.Failure<int>("Port.NotFound", "بورت الجهاز غير موجود.");

        if (port.SimCard != null && !port.SimCard.IsDelete)
            return Result.Failure<int>("Port.Occupied", $"البورت {port.PortLabel} يحتوي بالفعل على شريحة مفعلة.");

        var sim = new SimCard
        {
            DevicePortId = port.Id,
            Operator = command.Operator,
            PhoneNumber = command.PhoneNumber,
            SimNumber = command.SimNumber,
            ICCID = command.ICCID,
            WalletPassword = command.WalletPassword,
            UssdPassword = command.UssdPassword,
            DailyLimit = command.DailyLimit,
            MonthlyLimit = command.MonthlyLimit,
            CurrentBalance = command.InitialBalance,
            PreviousBalance = command.InitialBalance,
            Notes = command.Notes,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        port.Status = PortStatus.Active;

        await _unitOfWork.SimCards.AddAsync(sim, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update Device active ports count
        var device = await _unitOfWork.Devices.GetByIdAsync(port.DeviceId, cancellationToken);
        if (device != null)
        {
            var activeCount = await _unitOfWork.SimCards.CountAsync(s => s.DevicePort.DeviceId == port.DeviceId && s.IsActive && !s.IsDelete, cancellationToken);
            device.ActivePorts = activeCount;
            _unitOfWork.Devices.Update(device);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(sim.Id);
    }

    public async Task<Result<bool>> UpdateSimCardAsync(int id, string? phoneNumber, string? simNumber, string? iccid, OperatorType op, string? walletPassword, string? ussdPassword, decimal dailyLimit, decimal monthlyLimit, bool isActive, bool isBlocked, decimal currentBalance, string? notes, CancellationToken cancellationToken = default)
    {
        var sim = await _unitOfWork.SimCards.GetByIdAsync(id, cancellationToken);
        if (sim == null)
            return Result.Failure<bool>("Sim.NotFound", "الشريحة غير موجودة.");

        sim.PhoneNumber = phoneNumber;
        sim.SimNumber = simNumber;
        sim.ICCID = iccid;
        sim.Operator = op;
        if (!string.IsNullOrWhiteSpace(walletPassword)) sim.WalletPassword = walletPassword;
        if (!string.IsNullOrWhiteSpace(ussdPassword)) sim.UssdPassword = ussdPassword;
        sim.DailyLimit = dailyLimit;
        sim.MonthlyLimit = monthlyLimit;
        sim.CurrentBalance = currentBalance;
        sim.IsActive = isActive;
        sim.IsBlocked = isBlocked;
        sim.Notes = notes;
        sim.ModifiedDate = DateTime.UtcNow;

        _unitOfWork.SimCards.Update(sim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> UpdateLimitsAsync(int simId, decimal dailyLimit, decimal monthlyLimit, CancellationToken cancellationToken = default)
    {
        var sim = await _unitOfWork.SimCards.GetByIdAsync(simId, cancellationToken);
        if (sim == null)
            return Result.Failure<bool>("Sim.NotFound", "الشريحة غير موجودة.");

        sim.DailyLimit = dailyLimit;
        sim.MonthlyLimit = monthlyLimit;
        sim.ModifiedDate = DateTime.UtcNow;

        _unitOfWork.SimCards.Update(sim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> DeleteSimCardAsync(int id, CancellationToken cancellationToken = default)
    {
        var sim = await _unitOfWork.SimCards.GetByIdAsync(id, cancellationToken);
        if (sim == null)
            return Result.Failure<bool>("Sim.NotFound", "الشريحة غير موجودة.");

        sim.IsDelete = true;
        sim.ModifiedDate = DateTime.UtcNow;

        _unitOfWork.SimCards.Update(sim);

        var port = await _unitOfWork.DevicePorts.GetByIdAsync(sim.DevicePortId, cancellationToken);
        if (port != null)
        {
            port.Status = PortStatus.Empty;
            _unitOfWork.DevicePorts.Update(port);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> ToggleSimCardStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var sim = await _unitOfWork.SimCards.GetByIdAsync(id, cancellationToken);
        if (sim == null)
            return Result.Failure<bool>("Sim.NotFound", "الشريحة غير موجودة.");

        sim.IsActive = !sim.IsActive;
        sim.ModifiedDate = DateTime.UtcNow;
        _unitOfWork.SimCards.Update(sim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(sim.IsActive);
    }


    public async Task<Result<bool>> UpdateBalanceAsync(int simId, decimal newBalance, string? reason, CancellationToken cancellationToken = default)
    {
        var sim = await _unitOfWork.SimCards.GetByIdAsync(simId, cancellationToken);
        if (sim == null)
            return Result.Failure<bool>("Sim.NotFound", "الشريحة غير موجودة.");

        sim.PreviousBalance = sim.CurrentBalance;
        sim.CurrentBalance = newBalance;
        sim.LastBalanceCheck = DateTime.UtcNow;
        _unitOfWork.SimCards.Update(sim);

        var history = new SimBalanceHistory
        {
            SimCardId = sim.Id,
            BalanceBefore = sim.PreviousBalance,
            BalanceAfter = sim.CurrentBalance,
            ChangeAmount = sim.CurrentBalance - sim.PreviousBalance,
            Notes = reason ?? "Manual / System update",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.SimBalanceHistories.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifier.NotifySimBalanceChangedAsync(sim, cancellationToken);

        return Result.Success(true);
    }
}
