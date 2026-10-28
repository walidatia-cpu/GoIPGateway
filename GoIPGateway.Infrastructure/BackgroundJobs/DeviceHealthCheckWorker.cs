using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GoIPGateway.Infrastructure.BackgroundJobs;

public class DeviceHealthCheckWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeviceHealthCheckWorker> _logger;

    public DeviceHealthCheckWorker(IServiceScopeFactory scopeFactory, ILogger<DeviceHealthCheckWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeviceHealthCheckWorker started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                    var goipClient = scope.ServiceProvider.GetRequiredService<IGoipCommunicationClient>();
                    var notifier = scope.ServiceProvider.GetRequiredService<ISignalRNotifier>();

                    var devices = await context.Devices
                        .Where(d => d.IsActive && !d.IsDelete)
                        .ToListAsync(stoppingToken);

                    foreach (var device in devices)
                    {
                        var isOnline = await goipClient.CheckDeviceHealthAsync(device, stoppingToken);
                        var changed = device.IsOnline != isOnline;

                        device.IsOnline = isOnline;
                        device.LastHeartbeat = DateTime.UtcNow;

                        if (changed)
                        {
                            _logger.LogInformation("Device {DeviceName} ({IpAddress}) status changed to {Status}",
                                device.DeviceName, device.IpAddress, isOnline ? "Online" : "Offline");
                            await notifier.NotifyDeviceStatusChangedAsync(device, stoppingToken);
                        }
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DeviceHealthCheckWorker loop.");
                }

                // Wait 2 minutes between health checks (catch cancellation gracefully on app shutdown)
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }

        _logger.LogInformation("DeviceHealthCheckWorker stopped gracefully.");
    }
}
