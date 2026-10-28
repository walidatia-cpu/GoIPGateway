using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Features.SimCards.Commands;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Services.SimCards;

public interface ISimCardCommandService
{
    Task<Result<int>> RegisterSimCardAsync(RegisterSimCardCommand command, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateSimCardAsync(int id, string? phoneNumber, string? simNumber, string? iccid, OperatorType op, string? walletPassword, string? ussdPassword, decimal dailyLimit, decimal monthlyLimit, bool isActive, bool isBlocked, decimal currentBalance, string? notes, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateLimitsAsync(int simId, decimal dailyLimit, decimal monthlyLimit, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteSimCardAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<bool>> ToggleSimCardStatusAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateBalanceAsync(int simId, decimal newBalance, string? reason, CancellationToken cancellationToken = default);
}
