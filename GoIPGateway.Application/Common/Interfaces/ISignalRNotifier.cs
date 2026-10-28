using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Common.Interfaces;

public interface ISignalRNotifier
{
    Task NotifyTransactionUpdatedAsync(TenantTransaction transaction, CancellationToken cancellationToken = default);
    Task NotifySimBalanceChangedAsync(SimCard simCard, CancellationToken cancellationToken = default);
    Task NotifyPortStatusChangedAsync(DevicePort port, CancellationToken cancellationToken = default);
    Task NotifyDeviceStatusChangedAsync(Device device, CancellationToken cancellationToken = default);
    Task SendLiveFeedMessageAsync(string eventType, object payload, int? tenantId = null, CancellationToken cancellationToken = default);
}
