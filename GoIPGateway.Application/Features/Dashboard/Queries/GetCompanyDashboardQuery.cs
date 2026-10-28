using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Dashboard;
using GoIPGateway.Application.Services.Dashboard;
using MediatR;

namespace GoIPGateway.Application.Features.Dashboard.Queries;

public record GetCompanyDashboardQuery(int? TenantId = null) : IRequest<Result<CompanyDashboardDto>>;

public class GetCompanyDashboardQueryHandler : IRequestHandler<GetCompanyDashboardQuery, Result<CompanyDashboardDto>>
{
    private readonly IDashboardQueryService _dashboardService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetCompanyDashboardQueryHandler(
        IDashboardQueryService dashboardService,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<CompanyDashboardDto>> Handle(GetCompanyDashboardQuery request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
        {
            return Result.Failure<CompanyDashboardDto>("Tenant.NotResolved", "تعذر تحديد حساب الشركة الحالي.");
        }

        return await _dashboardService.GetCompanyDashboardDataAsync(tenantId.Value, cancellationToken);
    }
}
