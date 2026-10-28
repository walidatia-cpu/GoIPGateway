using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GoIPGateway.Infrastructure.BackgroundJobs;

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

        try
        {
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
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in LimitResetWorker.");
                }

                // Check every 30 minutes (catch cancellation gracefully on app shutdown)
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
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

        _logger.LogInformation("LimitResetWorker stopped gracefully.");
    }
}
