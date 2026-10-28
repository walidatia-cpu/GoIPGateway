using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Dashboard;
using GoIPGateway.Application.Services.Dashboard;
using MediatR;

namespace GoIPGateway.Application.Features.Dashboard.Queries;

public record GetAdminDashboardQuery : IRequest<Result<AdminDashboardDto>>;

public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, Result<AdminDashboardDto>>
{
    private readonly IDashboardQueryService _dashboardService;

    public GetAdminDashboardQueryHandler(IDashboardQueryService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<Result<AdminDashboardDto>> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        return await _dashboardService.GetAdminDashboardDataAsync(cancellationToken);
    }
}
