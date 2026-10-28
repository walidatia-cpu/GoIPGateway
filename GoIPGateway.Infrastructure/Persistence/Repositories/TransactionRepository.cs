using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Infrastructure.Persistence.Repositories;

public class TransactionRepository : GenericRepository<TenantTransaction, long>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<TenantTransaction?> GetByRefWithLogsAsync(string transactionRef, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Logs.OrderBy(l => l.StepOrder))
            .Include(t => t.Tenant)
            .Include(t => t.ServiceDefinition)
            .Include(t => t.SimCard)
            .Include(t => t.DevicePort)
            .FirstOrDefaultAsync(t => t.TransactionRef == transactionRef, cancellationToken);
    }

    public async Task<(IReadOnlyList<TenantTransaction> Items, int TotalCount)> GetPagedTransactionsAsync(
        int? tenantId,
        int? simCardId,
        TransactionStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        if (simCardId.HasValue)
            query = query.Where(t => t.SimCardId == simCardId.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(t => t.RequestedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.RequestedAt <= toDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(t => t.Tenant)
            .Include(t => t.ServiceDefinition)
            .Include(t => t.SimCard)
            .OrderByDescending(t => t.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
