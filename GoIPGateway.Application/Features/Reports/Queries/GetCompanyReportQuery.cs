using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Reports;
using GoIPGateway.Application.Services.Reports;
using MediatR;

namespace GoIPGateway.Application.Features.Reports.Queries;

public record GetCompanyReportQuery(CompanyReportFilterDto Filter) : IRequest<Result<CompanyReportDto>>;

public class GetCompanyReportQueryHandler : IRequestHandler<GetCompanyReportQuery, Result<CompanyReportDto>>
{
    private readonly IReportQueryService _reportQueryService;

    public GetCompanyReportQueryHandler(IReportQueryService reportQueryService)
    {
        _reportQueryService = reportQueryService;
    }

    public async Task<Result<CompanyReportDto>> Handle(GetCompanyReportQuery request, CancellationToken cancellationToken)
    {
        return await _reportQueryService.GetCompanyReportAsync(request.Filter, cancellationToken);
    }
}
