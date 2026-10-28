using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GoIPGateway.Infrastructure.Services;

public class SignalRNotifier : ISignalRNotifier
{
    private readonly IHubContext<GatewayHub> _hubContext;
    private readonly ILogger<SignalRNotifier> _logger;

    public SignalRNotifier(IHubContext<GatewayHub> hubContext, ILogger<SignalRNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyTransactionUpdatedAsync(TenantTransaction transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                transaction.Id,
                transaction.TransactionRef,
                transaction.ExternalRef,
                transaction.TenantId,
                transaction.Status,
                StatusName = transaction.Status.ToString(),
                transaction.Amount,
                transaction.SimBalanceBefore,
                transaction.SimBalanceAfter,
                transaction.RawResponse,
                transaction.ErrorMessage,
                transaction.CompletedAt
            };

            // Notify specific tenant and admins
            await _hubContext.Clients.Group($"Tenant_{transaction.TenantId}").SendAsync("OnTransactionUpdated", payload, cancellationToken);
            await _hubContext.Clients.Group("SuperAdmins").SendAsync("OnTransactionUpdated", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR transaction update for {TxRef}", transaction.TransactionRef);
        }
    }

    public async Task NotifySimBalanceChangedAsync(SimCard simCard, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                simCard.Id,
                simCard.PhoneNumber,
                simCard.Operator,
                OperatorName = simCard.Operator.ToString(),
                simCard.CurrentBalance,
                simCard.PreviousBalance,
                simCard.LastBalanceCheck
            };

            await _hubContext.Clients.All.SendAsync("OnSimBalanceChanged", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR SIM balance change for SIM {SimId}", simCard.Id);
        }
    }

    public async Task NotifyPortStatusChangedAsync(DevicePort port, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                port.Id,
                port.DeviceId,
                port.PortNumber,
                port.PortLabel,
                port.Status,
                StatusName = port.Status.ToString(),
                port.IsBusy,
                port.BusySince
            };

            await _hubContext.Clients.All.SendAsync("OnPortStatusChanged", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR port status change for Port {PortId}", port.Id);
        }
    }

    public async Task NotifyDeviceStatusChangedAsync(Device device, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                device.Id,
                device.DeviceName,
                device.IsOnline,
                device.LastHeartbeat,
                device.ActivePorts,
                device.TotalPorts
            };

            await _hubContext.Clients.All.SendAsync("OnDeviceStatusChanged", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR device status change for Device {DeviceId}", device.Id);
        }
    }

    public async Task SendLiveFeedMessageAsync(string eventType, object payload, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = new
            {
                EventType = eventType,
                Data = payload,
                Timestamp = DateTime.UtcNow
            };

            if (tenantId.HasValue)
            {
                await _hubContext.Clients.Group($"Tenant_{tenantId.Value}").SendAsync("OnLiveFeed", msg, cancellationToken);
            }

            await _hubContext.Clients.Group("SuperAdmins").SendAsync("OnLiveFeed", msg, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send live feed message");
        }
    }
}
