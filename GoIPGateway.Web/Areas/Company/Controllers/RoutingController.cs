using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator")]
public class RoutingController : Controller
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public RoutingController(IAppDbContext context, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    private async Task<int?> ResolveTenantIdAsync()
    {
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (tenantId.HasValue) return tenantId.Value;

        var userId = _currentUser.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            var tu = await _context.TenantUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (tu != null) return tu.TenantId;
        }

        var defaultTenant = await _context.Tenants.FirstOrDefaultAsync(t => t.IsActive && !t.IsDelete);
        return defaultTenant?.Id;
    }

    private async Task<List<int>> GetCompanyDeviceIdsAsync(int tenantId)
    {
        return await _context.TenantDevices
            .Where(td => td.TenantId == tenantId && td.IsActive && !td.Device.IsDelete)
            .Select(td => td.DeviceId)
            .ToListAsync();
    }

    public async Task<IActionResult> Index(int? serviceId, int? simId, string tab = "byService")
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        // 1. Get Subscribed Services (Only services allowed to this tenant by admin)
        var tenantServices = await _context.TenantServices
            .Include(ts => ts.ServiceDefinition)
            .Where(ts => ts.TenantId == tenantId.Value && ts.IsActive && !ts.ServiceDefinition.IsDelete)
            .OrderBy(ts => ts.ServiceDefinition.Category)
            .ThenBy(ts => ts.ServiceDefinition.SortOrder)
            .ToListAsync();

        // 2. Get Company SIM Cards
        var deviceIds = await GetCompanyDeviceIdsAsync(tenantId.Value);
        var sims = await _context.SimCards
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .Where(s => deviceIds.Contains(s.DevicePort.DeviceId) && s.IsActive && !s.IsDelete)
            .OrderBy(s => s.DevicePort.Device.DeviceName)
            .ThenBy(s => s.DevicePort.PortNumber)
            .ToListAsync();

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
    public async Task<IActionResult> SaveByService(int serviceDefinitionId, int[]? selectedSimIds)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var tenantService = await _context.TenantServices
            .FirstOrDefaultAsync(ts => ts.TenantId == tenantId.Value && ts.ServiceDefinitionId == serviceDefinitionId && ts.IsActive);

        if (tenantService == null)
        {
            TempData["Error"] = "الخدمة المحددة غير مفعلة لحساب شركتك.";
            return RedirectToAction(nameof(Index), new { tab = "byService" });
        }

        tenantService.AssignedSimCardIds = selectedSimIds?.ToList() ?? new List<int>();
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم بنجاح تحديث الشرائح المعينة لتنفيذ هذه الخدمة.";
        return RedirectToAction(nameof(Index), new { serviceId = serviceDefinitionId, tab = "byService" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBySim(int simId, int[]? selectedServiceIds)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var deviceIds = await GetCompanyDeviceIdsAsync(tenantId.Value);
        var sim = await _context.SimCards
            .Include(s => s.DevicePort)
            .FirstOrDefaultAsync(s => s.Id == simId && deviceIds.Contains(s.DevicePort.DeviceId) && !s.IsDelete);

        if (sim == null)
        {
            TempData["Error"] = "الشريحة المحددة لا تنتمي لأجهزة شركتك.";
            return RedirectToAction(nameof(Index), new { tab = "bySim" });
        }

        var tenantServices = await _context.TenantServices
            .Where(ts => ts.TenantId == tenantId.Value && ts.IsActive)
            .ToListAsync();

        var selectedSet = (selectedServiceIds ?? Array.Empty<int>()).ToHashSet();

        foreach (var ts in tenantServices)
        {
            var simIds = ts.AssignedSimCardIds;
            if (selectedSet.Contains(ts.ServiceDefinitionId))
            {
                if (!simIds.Contains(simId))
                {
                    simIds.Add(simId);
                    ts.AssignedSimCardIds = simIds;
                }
            }
            else
            {
                if (simIds.Contains(simId))
                {
                    simIds.Remove(simId);
                    ts.AssignedSimCardIds = simIds;
                }
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"تم بنجاح تحديث وتعيين الخدمات المصرح بتنفيذها على شريحة '{sim.PhoneNumber}'.";
        return RedirectToAction(nameof(Index), new { simId = simId, tab = "bySim" });
    }
}
