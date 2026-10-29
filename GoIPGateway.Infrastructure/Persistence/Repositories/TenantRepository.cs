using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Infrastructure.Persistence.Repositories;

public class TenantRepository : GenericRepository<Tenant, int>, ITenantRepository
{
    public TenantRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.ApiKeys)
            .Include(t => t.AssignedDevices)
            .Include(t => t.SubscribedServices)
            .Where(t => !t.IsDelete)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.ApiKeys)
            .Include(t => t.Users)
            .Include(t => t.AssignedDevices)
                .ThenInclude(ad => ad.Device)
            .Include(t => t.SubscribedServices)
                .ThenInclude(ts => ts.ServiceDefinition)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDelete, cancellationToken);
    }

    public async Task<Tenant?> GetByApiKeyAsync(string apiKey, string secretKey, CancellationToken cancellationToken = default)
    {
        var key = await _context.TenantApiKeys
            .Include(k => k.Tenant)
            .FirstOrDefaultAsync(k => k.ApiKey == apiKey && k.SecretKey == secretKey && k.IsActive, cancellationToken);

        return key?.Tenant;
    }

    public async Task<IReadOnlyList<Tenant>> GetActiveTenantsWithStatsAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var query = _dbSet.AsQueryable();
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        else
        {
            query = query.Where(t => !t.IsDelete);
        }

        return await query
            .Include(t => t.ApiKeys)
            .Include(t => t.Transactions.Where(tx => tx.RequestedAt >= today))
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync(cancellationToken);
    }
}
