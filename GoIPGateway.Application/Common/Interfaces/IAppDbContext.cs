using GoIPGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace GoIPGateway.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantApiKey> TenantApiKeys { get; }
    DbSet<TenantUser> TenantUsers { get; }
    DbSet<Device> Devices { get; }
    DbSet<TenantDevice> TenantDevices { get; }
    DbSet<DevicePort> DevicePorts { get; }
    DbSet<SimCard> SimCards { get; }
    DbSet<ServiceDefinition> ServiceDefinitions { get; }
    DbSet<ServiceUssdStep> ServiceUssdSteps { get; }
    DbSet<ServiceParameter> ServiceParameters { get; }
    DbSet<TenantService> TenantServices { get; }
    DbSet<TenantTransaction> TenantTransactions { get; }
    DbSet<TransactionLog> TransactionLogs { get; }
    DbSet<SmsHistory> SmsHistories { get; }
    DbSet<SimBalanceHistory> SimBalanceHistories { get; }
    DbSet<TenantBalanceMovement> TenantBalanceMovements { get; }
    DbSet<PricingPlan> PricingPlans { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
