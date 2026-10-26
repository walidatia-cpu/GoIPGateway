using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Factories;

public interface IServiceExecutor
{
    bool CanHandle(ServiceType serviceType);
    Task<Result<TransactionResultDto>> ExecuteAsync(TenantTransaction transaction, CancellationToken cancellationToken = default);
}

public interface IServiceExecutorFactory
{
    IServiceExecutor GetExecutor(ServiceType serviceType);
}

public class ServiceExecutorFactory : IServiceExecutorFactory
{
    private readonly IEnumerable<IServiceExecutor> _executors;

    public ServiceExecutorFactory(IEnumerable<IServiceExecutor> executors)
    {
        _executors = executors;
    }

    public IServiceExecutor GetExecutor(ServiceType serviceType)
    {
        var executor = _executors.FirstOrDefault(e => e.CanHandle(serviceType));
        if (executor == null)
        {
            throw new NotSupportedException($"No executor registered for ServiceType: {serviceType}");
        }
        return executor;
    }
}
