using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    int? TenantId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
    string? AllowedServices { get; }
}

public interface ITenantProvider
{
    int? GetCurrentTenantId();
    void SetCurrentTenantId(int tenantId);
    bool IsMultiTenant { get; }
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

public interface ISignalRNotifier
{
    Task NotifyTransactionUpdatedAsync(TenantTransaction transaction, CancellationToken cancellationToken = default);
    Task NotifySimBalanceChangedAsync(SimCard simCard, CancellationToken cancellationToken = default);
    Task NotifyPortStatusChangedAsync(DevicePort port, CancellationToken cancellationToken = default);
    Task NotifyDeviceStatusChangedAsync(Device device, CancellationToken cancellationToken = default);
    Task SendLiveFeedMessageAsync(string eventType, object payload, int? tenantId = null, CancellationToken cancellationToken = default);
}

public interface IWebhookDispatcher
{
    Task<bool> DispatchTransactionWebhookAsync(TenantTransaction transaction, CancellationToken cancellationToken = default);
}

public interface IPortLockManager
{
    Task<bool> TryAcquireLockAsync(int devicePortId, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task ReleaseLockAsync(int devicePortId);
    bool IsPortLocked(int devicePortId);
}

public interface IGoipCommunicationClient
{
    Task<string> SendUssdCommandAsync(Device device, int portNumber, string ussdCommand, int timeoutSeconds = 30, CancellationToken cancellationToken = default);
    Task<bool> CheckDeviceHealthAsync(Device device, CancellationToken cancellationToken = default);
    Task<List<SmsHistory>> PollSmsMessagesAsync(Device device, CancellationToken cancellationToken = default);
}
