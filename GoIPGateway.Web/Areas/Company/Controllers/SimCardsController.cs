using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator")]
public class SimCardsController : Controller
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public SimCardsController(IAppDbContext context, ICurrentUserService currentUser, ITenantProvider tenantProvider)
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

    public async Task<IActionResult> Index()
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var deviceIds = await GetCompanyDeviceIdsAsync(tenantId.Value);

        var sims = await _context.SimCards
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .Where(s => deviceIds.Contains(s.DevicePort.DeviceId) && !s.IsDelete)
            .OrderBy(s => s.DevicePort.Device.DeviceName)
            .ThenBy(s => s.DevicePort.PortNumber)
            .ToListAsync();

        ViewBag.TotalBalance = sims.Where(s => s.IsActive).Sum(s => s.CurrentBalance);
        ViewBag.ActiveCount = sims.Count(s => s.IsActive);

        return View(sims);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? portId)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var deviceIds = await GetCompanyDeviceIdsAsync(tenantId.Value);

        var emptyPorts = await _context.DevicePorts
            .Include(p => p.Device)
            .Where(p => deviceIds.Contains(p.DeviceId) && p.SimCard == null && p.IsActive)
            .OrderBy(p => p.Device.DeviceName)
            .ThenBy(p => p.PortNumber)
            .ToListAsync();

        if (!emptyPorts.Any())
        {
            TempData["Error"] = "لا توجد منافذ فارغة متاحة في أجهزتك لتركيب شريحة جديدة. يرجى إضافة جهاز جديد أو تفريغ أحد المنافذ.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.EmptyPorts = emptyPorts;
        ViewBag.SelectedPortId = portId;

        return View(new SimCard
        {
            DevicePortId = portId ?? emptyPorts.First().Id,
            Operator = OperatorType.Vodafone,
            DailyLimit = 50000,
            MonthlyLimit = 500000,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SimCard sim)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var deviceIds = await GetCompanyDeviceIdsAsync(tenantId.Value);

        var port = await _context.DevicePorts
            .Include(p => p.Device)
            .FirstOrDefaultAsync(p => p.Id == sim.DevicePortId && deviceIds.Contains(p.DeviceId));

        if (port == null)
        {
            ModelState.AddModelError("DevicePortId", "المنفذ المختار غير صالح أو لا ينتمي لأجهزتك");
            return View(sim);
        }

        sim.CreatedDate = DateTime.UtcNow;
        sim.IsActive = true;
        sim.SimNumber = sim.PhoneNumber;

        port.Status = PortStatus.Active;

        _context.SimCards.Add(sim);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"تمت إضافة شريحة '{sim.PhoneNumber}' بنجاح وتعيينها للمنفذ {port.PortLabel}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var deviceIds = await GetCompanyDeviceIdsAsync(tenantId.Value);

        var sim = await _context.SimCards
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .FirstOrDefaultAsync(s => s.Id == id && deviceIds.Contains(s.DevicePort.DeviceId) && !s.IsDelete);

        if (sim == null) return NotFound();

        return View(sim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SimCard model)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var deviceIds = await GetCompanyDeviceIdsAsync(tenantId.Value);

        var sim = await _context.SimCards
            .Include(s => s.DevicePort)
            .FirstOrDefaultAsync(s => s.Id == id && deviceIds.Contains(s.DevicePort.DeviceId) && !s.IsDelete);

        if (sim == null) return NotFound();

        sim.PhoneNumber = model.PhoneNumber;
        sim.SimNumber = model.PhoneNumber;
        sim.Operator = model.Operator;
        sim.WalletPassword = model.WalletPassword;
        sim.CurrentBalance = model.CurrentBalance;
        sim.DailyLimit = model.DailyLimit;
        sim.MonthlyLimit = model.MonthlyLimit;
        sim.IsActive = model.IsActive;
        sim.Notes = model.Notes;
        sim.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"تم تحديث بيانات شريحة '{sim.PhoneNumber}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var deviceIds = await GetCompanyDeviceIdsAsync(tenantId.Value);

        var sim = await _context.SimCards
            .Include(s => s.DevicePort)
            .FirstOrDefaultAsync(s => s.Id == id && deviceIds.Contains(s.DevicePort.DeviceId) && !s.IsDelete);

        if (sim != null)
        {
            sim.IsDelete = true;
            sim.DevicePort.Status = PortStatus.Empty;
            await _context.SaveChangesAsync();
            TempData["Success"] = "تمت إزالة الشريحة بنجاح وأصبح المنفذ متاحاً.";
        }

        return RedirectToAction(nameof(Index));
    }
}
