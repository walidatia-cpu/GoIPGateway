using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Features.Tenants.Commands;
using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Services.Tenants;

public interface ITenantQueryService
{
    Task<Result<Tenant>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Tenant>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Tenant>>> GetActiveTenantsWithStatsAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TenantApiKey>>> GetApiKeysAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Result<Tenant>> GetTenantForApiSettingsAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Result<Tenant>> ValidateApiKeyAsync(string apiKey, string secretKey, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TenantService>>> GetTenantServicesWithDefinitionsAsync(int tenantId, CancellationToken cancellationToken = default);
}

