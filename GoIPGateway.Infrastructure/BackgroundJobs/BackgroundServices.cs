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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeviceHealthCheckWorker loop.");
            }

            // Wait 2 minutes between health checks
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}

public class LimitResetWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LimitResetWorker> _logger;

    public LimitResetWorker(IServiceScopeFactory scopeFactory, ILogger<LimitResetWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LimitResetWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if midnight
                var now = DateTime.UtcNow;
                if (now.Hour == 0 && now.Minute < 5)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                    var sims = await context.SimCards.Where(s => s.DailyConsumed > 0).ToListAsync(stoppingToken);
                    foreach (var sim in sims)
                    {
                        sim.DailyConsumed = 0;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Daily SIM card limits reset successfully at {Time}", now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LimitResetWorker.");
            }

            // Check every 30 minutes
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
