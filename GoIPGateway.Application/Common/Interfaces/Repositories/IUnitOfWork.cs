using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Common.Interfaces.Repositories;

/// <summary>
/// Unit of Work pattern interface for orchestrating database transactions across repositories
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    ITenantRepository Tenants { get; }
    IDeviceRepository Devices { get; }
    IRepository<DevicePort, int> DevicePorts { get; }
    ISimCardRepository SimCards { get; }
    IServiceDefinitionRepository Services { get; }
    IRepository<ServiceUssdStep, int> ServiceSteps { get; }
    IRepository<ServiceParameter, int> ServiceParameters { get; }
    IRepository<TenantService, int> TenantServices { get; }
    IRepository<TenantApiKey, int> TenantApiKeys { get; }
    ITransactionRepository Transactions { get; }
    IRepository<TransactionLog, long> TransactionLogs { get; }
    IRepository<SimBalanceHistory, long> SimBalanceHistories { get; }
    IRepository<TenantBalanceMovement, long> TenantBalanceMovements { get; }
    IRepository<PricingPlan, int> PricingPlans { get; }

    IRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
