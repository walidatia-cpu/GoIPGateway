using System.Collections.Concurrent;
using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace GoIPGateway.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private readonly ConcurrentDictionary<string, object> _repositories = new();

    private ITenantRepository? _tenants;
    private IDeviceRepository? _devices;
    private IRepository<DevicePort, int>? _devicePorts;
    private ISimCardRepository? _simCards;
    private IServiceDefinitionRepository? _services;
    private IRepository<ServiceUssdStep, int>? _serviceSteps;
    private IRepository<ServiceParameter, int>? _serviceParameters;
    private IRepository<TenantService, int>? _tenantServices;
    private IRepository<TenantApiKey, int>? _tenantApiKeys;
    private ITransactionRepository? _transactions;
    private IRepository<TransactionLog, long>? _transactionLogs;
    private IRepository<SimBalanceHistory, long>? _simBalanceHistories;
    private IRepository<TenantBalanceMovement, long>? _tenantBalanceMovements;
    private IRepository<PricingPlan, int>? _pricingPlans;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public ITenantRepository Tenants => _tenants ??= new TenantRepository(_context);
    public IDeviceRepository Devices => _devices ??= new DeviceRepository(_context);
    public IRepository<DevicePort, int> DevicePorts => _devicePorts ??= new GenericRepository<DevicePort, int>(_context);
    public ISimCardRepository SimCards => _simCards ??= new SimCardRepository(_context);
    public IServiceDefinitionRepository Services => _services ??= new ServiceDefinitionRepository(_context);
    public IRepository<ServiceUssdStep, int> ServiceSteps => _serviceSteps ??= new GenericRepository<ServiceUssdStep, int>(_context);
    public IRepository<ServiceParameter, int> ServiceParameters => _serviceParameters ??= new GenericRepository<ServiceParameter, int>(_context);
    public IRepository<TenantService, int> TenantServices => _tenantServices ??= new GenericRepository<TenantService, int>(_context);
    public IRepository<TenantApiKey, int> TenantApiKeys => _tenantApiKeys ??= new GenericRepository<TenantApiKey, int>(_context);
    public ITransactionRepository Transactions => _transactions ??= new TransactionRepository(_context);
    public IRepository<TransactionLog, long> TransactionLogs => _transactionLogs ??= new GenericRepository<TransactionLog, long>(_context);
    public IRepository<SimBalanceHistory, long> SimBalanceHistories => _simBalanceHistories ??= new GenericRepository<SimBalanceHistory, long>(_context);
    public IRepository<TenantBalanceMovement, long> TenantBalanceMovements => _tenantBalanceMovements ??= new GenericRepository<TenantBalanceMovement, long>(_context);
    public IRepository<PricingPlan, int> PricingPlans => _pricingPlans ??= new GenericRepository<PricingPlan, int>(_context);

    public IRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class
    {
        var typeName = typeof(TEntity).Name;
        return (IRepository<TEntity, TKey>)_repositories.GetOrAdd(typeName, _ => new GenericRepository<TEntity, TKey>(_context));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
        }
        await _context.DisposeAsync();
    }
}
