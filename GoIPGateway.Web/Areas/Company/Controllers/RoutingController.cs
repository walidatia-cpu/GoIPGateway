using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Features.SimCards.Queries;
using GoIPGateway.Application.Features.Tenants.Commands;
using GoIPGateway.Application.Features.Tenants.Queries;
using GoIPGateway.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator")]
public class RoutingController : Controller
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public RoutingController(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    private async Task<int?> ResolveTenantIdAsync(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (tenantId.HasValue) return tenantId.Value;

        var tenantsResult = await _mediator.Send(new GetTenantsListQuery(), cancellationToken);
        if (tenantsResult.Succeeded && tenantsResult.Data != null)
        {
            var firstActive = tenantsResult.Data.FirstOrDefault(t => t.IsActive && !t.IsDelete);
            return firstActive?.Id;
        }

        return null;
    }

    public async Task<IActionResult> Index(int? serviceId, int? simId, string tab = "byService", CancellationToken cancellationToken = default)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        // 1. Get Subscribed Services
        var servicesResult = await _mediator.Send(new GetTenantServicesQuery(tenantId.Value), cancellationToken);
        var tenantServices = servicesResult.Succeeded ? servicesResult.Data ?? new List<TenantService>() : new List<TenantService>();

        // 2. Get Company SIM Cards
        var simsResult = await _mediator.Send(new GetCompanySimCardsQuery(tenantId.Value), cancellationToken);
        var sims = simsResult.Succeeded ? simsResult.Data ?? new List<SimCard>() : new List<SimCard>();

        // Active selection for Tab 1 (By Service)
        var activeServiceId = serviceId ?? tenantServices.FirstOrDefault()?.ServiceDefinitionId;
        var activeTenantService = tenantServices.FirstOrDefault(ts => ts.ServiceDefinitionId == activeServiceId);
        var assignedSimIdsForService = activeTenantService?.AssignedSimCardIds ?? new List<int>();

        // Active selection for Tab 2 (By SIM)
        var activeSimId = simId ?? sims.FirstOrDefault()?.Id;
        var allowedServiceIdsForSim = new List<int>();
        if (activeSimId.HasValue)
        {
            allowedServiceIdsForSim = tenantServices
                .Where(ts => ts.AssignedSimCardIds.Contains(activeSimId.Value))
                .Select(ts => ts.ServiceDefinitionId)
                .ToList();
        }

        ViewBag.TenantServices = tenantServices;
        ViewBag.CompanySims = sims;
        ViewBag.ActiveServiceId = activeServiceId;
        ViewBag.ActiveTenantService = activeTenantService;
        ViewBag.AssignedSimIdsForService = assignedSimIdsForService;
        ViewBag.ActiveSimId = activeSimId;
        ViewBag.AllowedServiceIdsForSim = allowedServiceIdsForSim;
        ViewBag.ActiveTab = tab;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveByService(int serviceDefinitionId, int[]? selectedSimIds, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var result = await _mediator.Send(new UpdateRoutingByServiceCommand(tenantId.Value, serviceDefinitionId, selectedSimIds), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم بنجاح تحديث الشرائح المعينة لتنفيذ هذه الخدمة.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشلت عملية حفظ التوجيه.";
        }

        return RedirectToAction(nameof(Index), new { serviceId = serviceDefinitionId, tab = "byService" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBySim(int simId, int[]? selectedServiceIds, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var result = await _mediator.Send(new UpdateRoutingBySimCommand(tenantId.Value, simId, selectedServiceIds), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم بنجاح تحديث وتعيين الخدمات المصرح بتنفيذها على الشريحة.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشلت عملية حفظ التوجيه.";
        }

        return RedirectToAction(nameof(Index), new { simId = simId, tab = "bySim" });
    }
}
