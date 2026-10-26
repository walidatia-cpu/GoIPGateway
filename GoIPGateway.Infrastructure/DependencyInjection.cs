using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Factories;
using GoIPGateway.Infrastructure.BackgroundJobs;
using GoIPGateway.Infrastructure.Data;
using GoIPGateway.Infrastructure.GoIP;
using GoIPGateway.Infrastructure.Identity;
using GoIPGateway.Infrastructure.Services;
using GoIPGateway.Infrastructure.Services.Executors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GoIPGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database Connection
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=GoIPGatewayDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // 2. Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

        // 3. HTTP Clients
        services.AddHttpClient<IGoipCommunicationClient, GoIPHttpClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IWebhookDispatcher, WebhookDispatcher>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // 4. Concurrency & Hardware Communication
        services.AddSingleton<IPortLockManager, PortLockManager>();

        // 5. Real-Time & Security
        services.AddScoped<ISignalRNotifier, SignalRNotifier>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // 6. Service Executors (Factory Strategies)
        services.AddScoped<UssdMultiStepExecutor>();
        services.AddScoped<IServiceExecutor, UssdMultiStepExecutor>(sp => sp.GetRequiredService<UssdMultiStepExecutor>());
        services.AddScoped<IServiceExecutor, BalanceInquiryExecutor>();
        services.AddScoped<IServiceExecutor, SimNumberInquiryExecutor>();
        services.AddScoped<IServiceExecutor, QrPaymentExecutor>();

        // 7. Background Hosted Workers
        services.AddHostedService<DeviceHealthCheckWorker>();
        services.AddHostedService<LimitResetWorker>();

        return services;
    }
}
