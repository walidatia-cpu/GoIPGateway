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
[Authorize(Roles = "TenantAdmin,TenantOperator,SuperAdmin")]
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

        var devices = sims
            .Where(s => s.DevicePort?.Device != null)
            .Select(s => s.DevicePort!.Device!)
            .DistinctBy(d => d.Id)
            .ToList();

        ViewBag.TenantServices = tenantServices;
        ViewBag.CompanySims = sims;
        ViewBag.Devices = devices;
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

    [HttpPost]
    public async Task<IActionResult> ToggleServiceSimAjax([FromBody] ToggleServiceSimRequest req, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Json(new { success = false, message = "غير مصرح" });

        var servicesResult = await _mediator.Send(new GetTenantServicesQuery(tenantId.Value), cancellationToken);
        var tenantService = servicesResult.Data?.FirstOrDefault(ts => ts.ServiceDefinitionId == req.ServiceId);
        if (tenantService == null) return Json(new { success = false, message = "الخدمة غير موجودة لحساب شركتك." });

        var simIds = tenantService.AssignedSimCardIds.ToList();
        if (req.Assign && !simIds.Contains(req.SimId))
        {
            simIds.Add(req.SimId);
        }
        else if (!req.Assign && simIds.Contains(req.SimId))
        {
            simIds.Remove(req.SimId);
        }

        var result = await _mediator.Send(new UpdateRoutingByServiceCommand(tenantId.Value, req.ServiceId, simIds.ToArray()), cancellationToken);
        return Json(new { success = result.Succeeded, count = simIds.Count, message = result.Succeeded ? "تم الحفظ تلقائياً" : result.Error?.Message });
    }

    [HttpPost]
    public async Task<IActionResult> SetServiceSimsAjax([FromBody] SetServiceSimsRequest req, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Json(new { success = false, message = "غير مصرح" });

        var result = await _mediator.Send(new UpdateRoutingByServiceCommand(tenantId.Value, req.ServiceId, req.SimIds), cancellationToken);
        return Json(new { success = result.Succeeded, count = req.SimIds?.Length ?? 0, message = result.Succeeded ? "تم تحديث تعيين الشرائح بنجاح" : result.Error?.Message });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleSimServiceAjax([FromBody] ToggleSimServiceRequest req, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Json(new { success = false, message = "غير مصرح" });

        var servicesResult = await _mediator.Send(new GetTenantServicesQuery(tenantId.Value), cancellationToken);
        var tenantServices = servicesResult.Data ?? new List<TenantService>();

        var allowedServiceIds = tenantServices
            .Where(ts => ts.AssignedSimCardIds.Contains(req.SimId))
            .Select(ts => ts.ServiceDefinitionId)
            .ToList();

        if (req.Assign && !allowedServiceIds.Contains(req.ServiceId))
        {
            allowedServiceIds.Add(req.ServiceId);
        }
        else if (!req.Assign && allowedServiceIds.Contains(req.ServiceId))
        {
            allowedServiceIds.Remove(req.ServiceId);
        }

        var result = await _mediator.Send(new UpdateRoutingBySimCommand(tenantId.Value, req.SimId, allowedServiceIds.ToArray()), cancellationToken);
        return Json(new { success = result.Succeeded, count = allowedServiceIds.Count, message = result.Succeeded ? "تم الحفظ تلقائياً" : result.Error?.Message });
    }

    [HttpPost]
    public async Task<IActionResult> SetSimServicesAjax([FromBody] SetSimServicesRequest req, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Json(new { success = false, message = "غير مصرح" });

        var result = await _mediator.Send(new UpdateRoutingBySimCommand(tenantId.Value, req.SimId, req.ServiceIds), cancellationToken);
        return Json(new { success = result.Succeeded, count = req.ServiceIds?.Length ?? 0, message = result.Succeeded ? "تم تحديث الخدمات المسندة للخط بنجاح" : result.Error?.Message });
    }
}

public class ToggleServiceSimRequest
{
    public int ServiceId { get; set; }
    public int SimId { get; set; }
    public bool Assign { get; set; }
}

public class SetServiceSimsRequest
{
    public int ServiceId { get; set; }
    public int[] SimIds { get; set; } = Array.Empty<int>();
}

public class ToggleSimServiceRequest
{
    public int SimId { get; set; }
    public int ServiceId { get; set; }
    public bool Assign { get; set; }
}

public class SetSimServicesRequest
{
    public int SimId { get; set; }
    public int[] ServiceIds { get; set; } = Array.Empty<int>();
}

