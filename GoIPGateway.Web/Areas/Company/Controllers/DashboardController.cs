using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Features.Dashboard.Queries;
using GoIPGateway.Application.Features.Tenants.Queries;
using GoIPGateway.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator,TenantViewer,SuperAdmin")]
public class DashboardController : Controller
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public DashboardController(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    private async Task<int?> ResolveTenantIdAsync(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (tenantId.HasValue) return tenantId.Value;

        // Fallback: get first active tenant
        var tenantsResult = await _mediator.Send(new GetTenantsListQuery(), cancellationToken);
        if (tenantsResult.Succeeded && tenantsResult.Data != null)
        {
            var firstActive = tenantsResult.Data.FirstOrDefault(t => t.IsActive && !t.IsDelete);
            return firstActive?.Id;
        }

        return null;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            return RedirectToAction("AccessDenied", "Account", new { area = "" });
        }

        var result = await _mediator.Send(new GetCompanyDashboardQuery(tenantId.Value), cancellationToken);
        if (!result.Succeeded || result.Data == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            return RedirectToAction("AccessDenied", "Account", new { area = "" });
        }

        var vm = CompanyDashboardViewModel.FromDto(result.Data);
        return View(vm);
    }
}