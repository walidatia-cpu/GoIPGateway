using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Features.Tenants.Commands;
using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Services.Tenants;

public interface ITenantCommandService
{
    Task<Result<int>> CreateTenantAsync(CreateTenantCommand command, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateTenantAsync(int id, string companyName, string? companyNameEn, string? contactPerson, string? contactEmail, string? contactPhone, string? address, string? taxId, int maxDevices, int maxDailyOperations, int maxMonthlyOps, decimal creditLimit, bool isActive, bool isBlocked, string? blockReason, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteTenantAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ApiKeyDto>> GenerateApiKeyAsync(int tenantId, string keyName, int rateLimit = 100, CancellationToken cancellationToken = default);
    Task<Result<bool>> CreateTenantTokenAsync(int tenantId, string keyName, int[]? allowedServiceIds, int rateLimit = 100, int expiryDays = 365, CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeApiKeyAsync(int apiKeyId, int tenantId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ActivateApiKeyAsync(int apiKeyId, int tenantId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteApiKeyAsync(int apiKeyId, int tenantId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ToggleBlockTenantAsync(int tenantId, string? reason = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> SaveTenantServicesAsync(int tenantId, int[]? selectedServiceIds, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateRoutingByServiceAsync(int tenantId, int serviceDefinitionId, int[]? selectedSimIds, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateRoutingBySimAsync(int tenantId, int simId, int[]? selectedServiceIds, CancellationToken cancellationToken = default);
}

