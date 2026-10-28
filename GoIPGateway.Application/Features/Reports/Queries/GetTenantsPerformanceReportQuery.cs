using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Reports;
using GoIPGateway.Application.Services.Reports;
using MediatR;

namespace GoIPGateway.Application.Features.Reports.Queries;

public record GetTenantsPerformanceReportQuery(TenantsPerformanceFilterDto Filter) : IRequest<Result<TenantsPerformanceReportDto>>;

public class GetTenantsPerformanceReportQueryHandler : IRequestHandler<GetTenantsPerformanceReportQuery, Result<TenantsPerformanceReportDto>>
{
    private readonly IReportQueryService _reportQueryService;

    public GetTenantsPerformanceReportQueryHandler(IReportQueryService reportQueryService)
    {
        _reportQueryService = reportQueryService;
    }

    public async Task<Result<TenantsPerformanceReportDto>> Handle(GetTenantsPerformanceReportQuery request, CancellationToken cancellationToken)
    {
        return await _reportQueryService.GetTenantsPerformanceReportAsync(request.Filter, cancellationToken);
    }
}
