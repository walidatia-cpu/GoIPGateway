using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Reports;

namespace GoIPGateway.Application.Services.Reports;

public interface IReportQueryService
{
    Task<Result<CompanyReportDto>> GetCompanyReportAsync(CompanyReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result<TenantsPerformanceReportDto>> GetTenantsPerformanceReportAsync(TenantsPerformanceFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result<ExecutiveSummaryDto>> GetExecutiveSummaryReportAsync(ExecutiveSummaryFilterDto filter, CancellationToken cancellationToken = default);
}
