using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Reports;
using GoIPGateway.Application.Services.Reports;
using MediatR;

namespace GoIPGateway.Application.Features.Reports.Queries;

public record GetExecutiveSummaryReportQuery(ExecutiveSummaryFilterDto Filter) : IRequest<Result<ExecutiveSummaryDto>>;

public class GetExecutiveSummaryReportQueryHandler : IRequestHandler<GetExecutiveSummaryReportQuery, Result<ExecutiveSummaryDto>>
{
    private readonly IReportQueryService _reportQueryService;

    public GetExecutiveSummaryReportQueryHandler(IReportQueryService reportQueryService)
    {
        _reportQueryService = reportQueryService;
    }

    public async Task<Result<ExecutiveSummaryDto>> Handle(GetExecutiveSummaryReportQuery request, CancellationToken cancellationToken)
    {
        return await _reportQueryService.GetExecutiveSummaryReportAsync(request.Filter, cancellationToken);
    }
}
