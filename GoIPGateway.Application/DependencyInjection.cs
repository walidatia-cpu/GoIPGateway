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

        return services;
    }
}
