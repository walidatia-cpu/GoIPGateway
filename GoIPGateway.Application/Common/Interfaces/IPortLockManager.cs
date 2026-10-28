namespace GoIPGateway.Application.Common.Interfaces;

public interface IPortLockManager
{
    Task<bool> TryAcquireLockAsync(int devicePortId, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task ReleaseLockAsync(int devicePortId);
    bool IsPortLocked(int devicePortId);
}
