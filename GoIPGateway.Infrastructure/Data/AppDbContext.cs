using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IAppDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantApiKey> TenantApiKeys => Set<TenantApiKey>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<TenantDevice> TenantDevices => Set<TenantDevice>();
    public DbSet<DevicePort> DevicePorts => Set<DevicePort>();
    public DbSet<SimCard> SimCards => Set<SimCard>();
    public DbSet<ServiceDefinition> ServiceDefinitions => Set<ServiceDefinition>();
    public DbSet<ServiceUssdStep> ServiceUssdSteps => Set<ServiceUssdStep>();
    public DbSet<ServiceParameter> ServiceParameters => Set<ServiceParameter>();
    public DbSet<TenantService> TenantServices => Set<TenantService>();
    public DbSet<TenantTransaction> TenantTransactions => Set<TenantTransaction>();
    public DbSet<TransactionLog> TransactionLogs => Set<TransactionLog>();
    public DbSet<SmsHistory> SmsHistories => Set<SmsHistory>();
    public DbSet<SimBalanceHistory> SimBalanceHistories => Set<SimBalanceHistory>();
    public DbSet<TenantBalanceMovement> TenantBalanceMovements => Set<TenantBalanceMovement>();
    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Decimal precision (18, 4) for financial balances and rates
        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18, 4)");
        }

        // Tenants
        builder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(t => t.CompanyName);
            entity.HasQueryFilter(t => !t.IsDelete);
        });

        // TenantApiKeys
        builder.Entity<TenantApiKey>(entity =>
        {
            entity.HasIndex(k => k.ApiKey).IsUnique();
            entity.HasOne(k => k.Tenant)
                .WithMany(t => t.ApiKeys)
                .HasForeignKey(k => k.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Devices & Ports
        builder.Entity<Device>(entity =>
        {
            entity.HasQueryFilter(d => !d.IsDelete);
        });

        builder.Entity<DevicePort>(entity =>
        {
            entity.HasIndex(p => new { p.DeviceId, p.PortNumber }).IsUnique();
            entity.HasOne(p => p.Device)
                .WithMany(d => d.Ports)
                .HasForeignKey(p => p.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TenantDevices
        builder.Entity<TenantDevice>(entity =>
        {
            entity.HasIndex(td => new { td.TenantId, td.DeviceId }).IsUnique();
            entity.HasOne(td => td.Tenant)
                .WithMany(t => t.AssignedDevices)
                .HasForeignKey(td => td.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(td => td.Device)
                .WithMany(d => d.AssignedTenants)
                .HasForeignKey(td => td.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SimCards
        builder.Entity<SimCard>(entity =>
        {
            entity.HasIndex(s => s.PhoneNumber);
            entity.HasQueryFilter(s => !s.IsDelete);
            entity.HasOne(s => s.DevicePort)
                .WithOne(p => p.SimCard)
                .HasForeignKey<SimCard>(s => s.DevicePortId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Services
        builder.Entity<ServiceDefinition>(entity =>
        {
            entity.HasIndex(s => s.ServiceCode).IsUnique();
            entity.HasQueryFilter(s => !s.IsDelete);
        });

        builder.Entity<ServiceUssdStep>(entity =>
        {
            entity.HasOne(step => step.ServiceDefinition)
                .WithMany(s => s.UssdSteps)
                .HasForeignKey(step => step.ServiceDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ServiceParameter>(entity =>
        {
            entity.HasOne(param => param.ServiceDefinition)
                .WithMany(s => s.Parameters)
                .HasForeignKey(param => param.ServiceDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TenantService>(entity =>
        {
            entity.HasIndex(ts => new { ts.TenantId, ts.ServiceDefinitionId }).IsUnique();
            entity.HasOne(ts => ts.Tenant)
                .WithMany(t => t.SubscribedServices)
                .HasForeignKey(ts => ts.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Transactions
        builder.Entity<TenantTransaction>(entity =>
        {
            entity.HasIndex(t => t.TransactionRef).IsUnique();
            entity.HasIndex(t => t.TenantId);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.RequestedAt);

            entity.HasOne(t => t.Tenant)
                .WithMany(ten => ten.Transactions)
                .HasForeignKey(t => t.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ServiceDefinition)
                .WithMany(s => s.Transactions)
                .HasForeignKey(t => t.ServiceDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.SimCard)
                .WithMany(s => s.Transactions)
                .HasForeignKey(t => t.SimCardId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.DevicePort)
                .WithMany(p => p.Transactions)
                .HasForeignKey(t => t.DevicePortId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TransactionLog>(entity =>
        {
            entity.HasOne(l => l.Transaction)
                .WithMany(t => t.Logs)
                .HasForeignKey(l => l.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService?.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
                entry.Entity.CreatedById ??= currentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedDate = now;
                entry.Entity.ModifiedById = currentUserId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDelete = true;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
