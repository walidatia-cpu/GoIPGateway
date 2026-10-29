using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Common.Interfaces.Repositories;

public interface ITenantRepository : IRepository<Tenant, int>
{
    Task<Tenant?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByApiKeyAsync(string apiKey, string secretKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> GetActiveTenantsWithStatsAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);
}
