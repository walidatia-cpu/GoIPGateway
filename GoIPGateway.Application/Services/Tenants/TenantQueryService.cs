using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Services.Tenants;

public class TenantQueryService : ITenantQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public TenantQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Tenant>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetWithDetailsAsync(id, cancellationToken);
        if (tenant == null)
            return Result.Failure<Tenant>("Tenant.NotFound", "الشركة غير موجودة.");

        return Result.Success(tenant);
    }

    public async Task<Result<IReadOnlyList<Tenant>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _unitOfWork.Tenants.GetAllAsync(cancellationToken);
        return Result.Success(tenants);
    }

    public async Task<Result<IReadOnlyList<Tenant>>> GetActiveTenantsWithStatsAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var tenants = await _unitOfWork.Tenants.GetActiveTenantsWithStatsAsync(includeDeleted, cancellationToken);
        return Result.Success(tenants);
    }

    public async Task<Result<IReadOnlyList<TenantApiKey>>> GetApiKeysAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var keys = await _unitOfWork.TenantApiKeys.FindAsync(k => k.TenantId == tenantId && k.IsActive, cancellationToken);
        return Result.Success(keys);
    }

    public async Task<Result<Tenant>> GetTenantForApiSettingsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.AsQueryable()
            .Include(t => t.ApiKeys)
            .Include(t => t.SubscribedServices)
                .ThenInclude(ts => ts.ServiceDefinition)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
            return Result.Failure<Tenant>("Tenant.NotFound", "الشركة غير موجودة.");

        return Result.Success(tenant);
    }

    public async Task<Result<Tenant>> ValidateApiKeyAsync(string apiKey, string secretKey, CancellationToken cancellationToken = default)
    {
        var key = await _unitOfWork.TenantApiKeys.AsQueryable()
            .Include(k => k.Tenant)
            .FirstOrDefaultAsync(k => k.ApiKey == apiKey && k.SecretKey == secretKey && k.IsActive, cancellationToken);

        if (key == null || !key.Tenant.IsActive || key.Tenant.IsBlocked)
        {
            return Result.Failure<Tenant>("Auth.InvalidKeys", "مفتاح API غير صالح أو أن حساب الشركة موقوف.");
        }

        key.LastUsedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(key.Tenant);
    }

    public async Task<Result<IReadOnlyList<TenantService>>> GetTenantServicesWithDefinitionsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var services = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _unitOfWork.TenantServices.AsQueryable()
                .AsNoTracking()
                .Include(ts => ts.ServiceDefinition)
                .Where(ts => ts.TenantId == tenantId && ts.IsActive && !ts.ServiceDefinition.IsDelete)
                .OrderBy(ts => ts.ServiceDefinition.Category)
                .ThenBy(ts => ts.ServiceDefinition.SortOrder),
            cancellationToken);

        return Result.Success<IReadOnlyList<TenantService>>(services);
    }
}

