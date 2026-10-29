using System.Security.Cryptography;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Features.Tenants.Commands;
using GoIPGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Services.Tenants;

public class TenantCommandService : ITenantCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public TenantCommandService(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> CreateTenantAsync(CreateTenantCommand command, CancellationToken cancellationToken = default)
    {
        var tenant = new Tenant
        {
            CompanyName = command.CompanyName,
            CompanyNameEn = command.CompanyNameEn,
            ContactPerson = command.ContactPerson,
            ContactEmail = command.ContactEmail,
            ContactPhone = command.ContactPhone,
            Address = command.Address,
            MaxDevices = command.MaxDevices,
            MaxDailyOperations = command.MaxDailyOperations,
            CreditLimit = command.CreditLimit,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedById = _currentUser.UserId
        };

        await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Automatically create a default production API Key
        var apiKey = new TenantApiKey
        {
            TenantId = tenant.Id,
            ApiKey = $"gw_live_{Guid.NewGuid():N}",
            SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            KeyName = "Default Production Key",
            RateLimit = 120,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.TenantApiKeys.AddAsync(apiKey, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(tenant.Id);
    }

    public async Task<Result<bool>> UpdateTenantAsync(int id, string companyName, string? companyNameEn, string? contactPerson, string? contactEmail, string? contactPhone, string? address, string? taxId, int maxDevices, int maxDailyOperations, int maxMonthlyOps, decimal creditLimit, bool isActive, bool isBlocked, string? blockReason, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(id, cancellationToken);
        if (tenant == null)
            return Result.Failure<bool>("Tenant.NotFound", "الشركة غير موجودة.");

        tenant.CompanyName = companyName.Trim();
        tenant.CompanyNameEn = companyNameEn?.Trim();
        tenant.ContactPerson = contactPerson?.Trim();
        tenant.ContactEmail = contactEmail?.Trim();
        tenant.ContactPhone = contactPhone?.Trim();
        tenant.Address = address?.Trim();
        tenant.TaxId = taxId?.Trim();
        tenant.MaxDevices = maxDevices > 0 ? maxDevices : 1;
        tenant.MaxDailyOperations = maxDailyOperations > 0 ? maxDailyOperations : 1000;
        tenant.MaxMonthlyOps = maxMonthlyOps > 0 ? maxMonthlyOps : 30000;
        tenant.CreditLimit = creditLimit;
        tenant.IsActive = isActive;
        tenant.IsBlocked = isBlocked;
        tenant.BlockReason = isBlocked ? blockReason?.Trim() : null;
        tenant.ModifiedDate = DateTime.UtcNow;
        tenant.ModifiedById = _currentUser.UserId;

        _unitOfWork.Tenants.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> DeleteTenantAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.AsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant == null)
            return Result.Failure<bool>("Tenant.NotFound", "الشركة غير موجودة.");

        tenant.IsDelete = true;
        tenant.IsActive = false;
        tenant.ModifiedDate = DateTime.UtcNow;
        tenant.ModifiedById = _currentUser.UserId;

        _unitOfWork.Tenants.Update(tenant);

        // Deactivate all active API keys for this tenant
        var keys = await _unitOfWork.TenantApiKeys.FindAsync(k => k.TenantId == tenant.Id && k.IsActive, cancellationToken);
        foreach (var key in keys)
        {
            key.IsActive = false;
            _unitOfWork.TenantApiKeys.Update(key);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> RestoreTenantAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.AsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant == null)
            return Result.Failure<bool>("Tenant.NotFound", "الشركة غير موجودة.");

        tenant.IsDelete = false;
        tenant.IsActive = true;
        tenant.ModifiedDate = DateTime.UtcNow;
        tenant.ModifiedById = _currentUser.UserId;

        _unitOfWork.Tenants.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<ApiKeyDto>> GenerateApiKeyAsync(int tenantId, string keyName, int rateLimit = 100, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            return Result.Failure<ApiKeyDto>("Tenant.NotFound", "الشركة غير موجودة.");

        var apiKey = new TenantApiKey
        {
            TenantId = tenant.Id,
            ApiKey = $"gw_{Guid.NewGuid():N}",
            SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            KeyName = keyName,
            RateLimit = rateLimit,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.TenantApiKeys.AddAsync(apiKey, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApiKeyDto
        {
            Id = apiKey.Id,
            ApiKey = apiKey.ApiKey,
            SecretKey = apiKey.SecretKey,
            KeyName = apiKey.KeyName,
            RateLimit = apiKey.RateLimit
        });
    }

    public async Task<Result<bool>> RevokeApiKeyAsync(int apiKeyId, int tenantId, CancellationToken cancellationToken = default)
    {
        var key = await _unitOfWork.TenantApiKeys.FirstOrDefaultAsync(k => k.Id == apiKeyId && k.TenantId == tenantId, cancellationToken);
        if (key == null)
            return Result.Failure<bool>("ApiKey.NotFound", "المفتاح غير موجود.");

        key.IsActive = false;
        _unitOfWork.TenantApiKeys.Update(key);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> CreateTenantTokenAsync(int tenantId, string keyName, int[]? allowedServiceIds, int rateLimit = 100, int expiryDays = 365, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            return Result.Failure<bool>("Tenant.NotFound", "الشركة غير موجودة.");

        string? allowedJson = null;
        if (allowedServiceIds != null && allowedServiceIds.Length > 0)
        {
            allowedJson = System.Text.Json.JsonSerializer.Serialize(allowedServiceIds);
        }

        var key = new TenantApiKey
        {
            TenantId = tenantId,
            KeyName = string.IsNullOrWhiteSpace(keyName) ? "API Token" : keyName.Trim(),
            ApiKey = "gw_" + Guid.NewGuid().ToString("N")[..16],
            SecretKey = "sec_" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            AllowedServices = allowedJson,
            RateLimit = rateLimit > 0 ? rateLimit : 100,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays > 0 ? expiryDays : 365)
        };

        await _unitOfWork.TenantApiKeys.AddAsync(key, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> ActivateApiKeyAsync(int apiKeyId, int tenantId, CancellationToken cancellationToken = default)
    {
        var key = await _unitOfWork.TenantApiKeys.FirstOrDefaultAsync(k => k.Id == apiKeyId && k.TenantId == tenantId, cancellationToken);
        if (key == null)
            return Result.Failure<bool>("ApiKey.NotFound", "المفتاح غير موجود.");

        key.IsActive = true;
        _unitOfWork.TenantApiKeys.Update(key);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> DeleteApiKeyAsync(int apiKeyId, int tenantId, CancellationToken cancellationToken = default)
    {
        var key = await _unitOfWork.TenantApiKeys.FirstOrDefaultAsync(k => k.Id == apiKeyId && k.TenantId == tenantId, cancellationToken);
        if (key == null)
            return Result.Failure<bool>("ApiKey.NotFound", "المفتاح غير موجود.");

        _unitOfWork.TenantApiKeys.Remove(key);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> SaveTenantServicesAsync(int tenantId, int[]? selectedServiceIds, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            return Result.Failure<bool>("Tenant.NotFound", "الشركة غير موجودة.");

        var existingServices = await _unitOfWork.TenantServices.FindAsync(ts => ts.TenantId == tenantId, cancellationToken);
        var selected = selectedServiceIds ?? Array.Empty<int>();

        var toRemove = existingServices.Where(ts => !selected.Contains(ts.ServiceDefinitionId)).ToList();
        if (toRemove.Count > 0)
        {
            _unitOfWork.TenantServices.RemoveRange(toRemove);
        }

        var existingIds = existingServices.Select(ts => ts.ServiceDefinitionId).ToHashSet();
        foreach (var sId in selected)
        {
            if (!existingIds.Contains(sId))
            {
                await _unitOfWork.TenantServices.AddAsync(new TenantService
                {
                    TenantId = tenantId,
                    ServiceDefinitionId = sId,
                    IsActive = true,
                    AssignedDate = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }

    public async Task<Result<bool>> ToggleBlockTenantAsync(int tenantId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            return Result.Failure<bool>("Tenant.NotFound", "الشركة غير موجودة.");

        tenant.IsBlocked = !tenant.IsBlocked;
        tenant.BlockReason = tenant.IsBlocked ? (reason ?? "حظر إداري") : null;
        tenant.ModifiedDate = DateTime.UtcNow;
        tenant.ModifiedById = _currentUser.UserId;

        _unitOfWork.Tenants.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> UpdateRoutingByServiceAsync(int tenantId, int serviceDefinitionId, int[]? selectedSimIds, CancellationToken cancellationToken = default)
    {
        var tenantService = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            _unitOfWork.TenantServices.AsQueryable()
                .Where(ts => ts.TenantId == tenantId && ts.ServiceDefinitionId == serviceDefinitionId && ts.IsActive),
            cancellationToken);

        if (tenantService == null)
            return Result.Failure<bool>("Service.NotAssigned", "الخدمة المحددة غير مفعلة لحساب شركتك.");

        tenantService.AssignedSimCardIds = selectedSimIds?.ToList() ?? new List<int>();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> UpdateRoutingBySimAsync(int tenantId, int simId, int[]? selectedServiceIds, CancellationToken cancellationToken = default)
    {
        var tenantServices = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _unitOfWork.TenantServices.AsQueryable()
                .Where(ts => ts.TenantId == tenantId && ts.IsActive),
            cancellationToken);

        var selectedSet = (selectedServiceIds ?? Array.Empty<int>()).ToHashSet();

        foreach (var ts in tenantServices)
        {
            var simIds = ts.AssignedSimCardIds.ToList();
            if (selectedSet.Contains(ts.ServiceDefinitionId))
            {
                if (!simIds.Contains(simId))
                {
                    simIds.Add(simId);
                    ts.AssignedSimCardIds = simIds;
                }
            }
            else
            {
                if (simIds.Contains(simId))
                {
                    simIds.Remove(simId);
                    ts.AssignedSimCardIds = simIds;
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}


