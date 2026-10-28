using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Infrastructure.Persistence.Repositories;

public class ServiceDefinitionRepository : GenericRepository<ServiceDefinition, int>, IServiceDefinitionRepository
{
    public ServiceDefinitionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<ServiceDefinition?> GetWithStepsAndParametersAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.UssdSteps.Where(st => !st.IsDelete).OrderBy(st => st.StepOrder))
            .Include(s => s.Parameters.Where(p => p.IsActive).OrderBy(p => p.SortOrder))
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceDefinition>> GetAllWithStepsAndParametersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.UssdSteps.Where(st => !st.IsDelete))
            .Include(s => s.Parameters.Where(p => p.IsActive))
            .Where(s => !s.IsDelete)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceDefinition>> GetSubscribedByTenantAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Parameters.Where(p => p.IsActive).OrderBy(p => p.SortOrder))
            .Where(s => !s.IsDelete && s.IsActive && s.TenantSubscriptions.Any(sub => sub.TenantId == tenantId && sub.IsActive))
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);
    }
}
