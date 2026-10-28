using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Dashboard;

namespace GoIPGateway.Application.Services.Dashboard;

public interface IDashboardQueryService
{
    Task<Result<AdminDashboardDto>> GetAdminDashboardDataAsync(CancellationToken cancellationToken = default);
    Task<Result<CompanyDashboardDto>> GetCompanyDashboardDataAsync(int tenantId, CancellationToken cancellationToken = default);
}
