using System.Collections.Concurrent;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoIPGateway.Infrastructure.GoIP;

public class PortLockManager : IPortLockManager
{
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PortLockManager> _logger;

    public PortLockManager(IServiceScopeFactory scopeFactory, ILogger<PortLockManager> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<bool> TryAcquireLockAsync(int devicePortId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(devicePortId, _ => new SemaphoreSlim(1, 1));

        var acquired = await semaphore.WaitAsync(timeout, cancellationToken);
        if (!acquired)
        {
            _logger.LogWarning("Failed to acquire in-memory semaphore for Port ID {PortId} within {TimeoutMs}ms", devicePortId, timeout.TotalMilliseconds);
            return false;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var port = await context.DevicePorts.FirstOrDefaultAsync(p => p.Id == devicePortId, cancellationToken);
            if (port == null)
            {
                semaphore.Release();
                return false;
            }

            // Check if locked in DB and not timed out (> 60s)
            if (port.IsBusy && port.BusySince.HasValue && DateTime.UtcNow - port.BusySince.Value < TimeSpan.FromSeconds(60))
            {
                _logger.LogWarning("Port ID {PortId} is flagged busy in database since {BusySince}", devicePortId, port.BusySince);
                semaphore.Release();
                return false;
            }

            port.IsBusy = true;
            port.BusySince = DateTime.UtcNow;
            port.Status = PortStatus.Busy;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully acquired lock for Port ID {PortId}", devicePortId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while setting DB lock for Port ID {PortId}", devicePortId);
            semaphore.Release();
            return false;
        }
    }

    public async Task ReleaseLockAsync(int devicePortId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var port = await context.DevicePorts.FirstOrDefaultAsync(p => p.Id == devicePortId);
            if (port != null)
            {
                port.IsBusy = false;
                port.BusySince = null;
                port.Status = port.SimCard != null ? PortStatus.Active : PortStatus.Empty;
                port.CurrentOperationId = null;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing DB lock status for Port ID {PortId}", devicePortId);
        }
        finally
        {
            if (_locks.TryGetValue(devicePortId, out var semaphore))
            {
                if (semaphore.CurrentCount == 0)
                {
                    semaphore.Release();
                }
            }
            _logger.LogInformation("Released lock for Port ID {PortId}", devicePortId);
        }
    }

    public bool IsPortLocked(int devicePortId)
    {
        if (_locks.TryGetValue(devicePortId, out var semaphore))
        {
            return semaphore.CurrentCount == 0;
        }
        return false;
    }
}
