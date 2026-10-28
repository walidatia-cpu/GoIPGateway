using System.Reflection;
using FluentValidation;
using GoIPGateway.Application.Common.Behaviors;
using GoIPGateway.Application.Factories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GoIPGateway.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Register FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Register Factories and Strategies
        services.AddTransient<IOperatorStrategy, VodafoneStrategy>();
        services.AddTransient<IOperatorStrategy, OrangeStrategy>();
        services.AddTransient<IOperatorStrategy, EtisalatStrategy>();
        services.AddTransient<IOperatorStrategy, WeStrategy>();
        services.AddSingleton<IOperatorStrategyFactory, OperatorStrategyFactory>();

        services.AddScoped<IServiceExecutorFactory, ServiceExecutorFactory>();

        // Register Module Services (Query Services & Command Services)
        services.AddScoped<Services.Tenants.ITenantQueryService, Services.Tenants.TenantQueryService>();
        services.AddScoped<Services.Tenants.ITenantCommandService, Services.Tenants.TenantCommandService>();

        services.AddScoped<Services.Devices.IDeviceQueryService, Services.Devices.DeviceQueryService>();
        services.AddScoped<Services.Devices.IDeviceCommandService, Services.Devices.DeviceCommandService>();

        services.AddScoped<Services.SimCards.ISimCardQueryService, Services.SimCards.SimCardQueryService>();
        services.AddScoped<Services.SimCards.ISimCardCommandService, Services.SimCards.SimCardCommandService>();

        services.AddScoped<Services.ServiceDefinitions.IServiceDefinitionQueryService, Services.ServiceDefinitions.ServiceDefinitionQueryService>();
        services.AddScoped<Services.ServiceDefinitions.IServiceDefinitionCommandService, Services.ServiceDefinitions.ServiceDefinitionCommandService>();

        services.AddScoped<Services.Transactions.ITransactionQueryService, Services.Transactions.TransactionQueryService>();
        services.AddScoped<Services.Transactions.ITransactionCommandService, Services.Transactions.TransactionCommandService>();

        services.AddScoped<Services.Dashboard.IDashboardQueryService, Services.Dashboard.DashboardQueryService>();
        services.AddScoped<Services.Reports.IReportQueryService, Services.Reports.ReportQueryService>();

        return services;
    }
}
