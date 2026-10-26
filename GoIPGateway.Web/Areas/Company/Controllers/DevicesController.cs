using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator")]
public class DevicesController : Controller
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public DevicesController(IAppDbContext context, ICurrentUserService currentUser, ITenantProvider tenantProvider)
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

    public async Task<IActionResult> Index()
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        var tenantDevices = await _context.TenantDevices
            .Include(td => td.Device)
                .ThenInclude(d => d.Ports)
                    .ThenInclude(p => p.SimCard)
            .Where(td => td.TenantId == tenantId.Value && td.IsActive && !td.Device.IsDelete)
            .ToListAsync();

        ViewBag.Tenant = tenant;
        ViewBag.CurrentCount = tenantDevices.Count;
        ViewBag.MaxDevices = tenant.MaxDevices;
        ViewBag.CanAddMore = tenantDevices.Count < tenant.MaxDevices;

        return View(tenantDevices);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        var currentCount = await _context.TenantDevices
            .CountAsync(td => td.TenantId == tenantId.Value && td.IsActive && !td.Device.IsDelete);

        if (currentCount >= tenant.MaxDevices)
        {
            TempData["Error"] = $"لقد وصلت للحد الأقصى المسموح به من الأجهزة في باقتك ({tenant.MaxDevices} أجهزة). يرجى التواصل مع إدارة النظام لزيادة عدد الأجهزة.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Tenant = tenant;
        ViewBag.CurrentCount = currentCount;
        ViewBag.MaxDevices = tenant.MaxDevices;

        return View(new Device
        {
            DeviceType = DeviceType.GoIP4,
            IpAddress = "192.168.1.100",
            Port = 80,
            HttpPort = 80,
            TelnetPort = 23,
            Username = "admin",
            Password = "admin"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Device device)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        var currentCount = await _context.TenantDevices
            .CountAsync(td => td.TenantId == tenantId.Value && td.IsActive && !td.Device.IsDelete);

        if (currentCount >= tenant.MaxDevices)
        {
            TempData["Error"] = $"لقد وصلت للحد الأقصى المسموح به من الأجهزة ({tenant.MaxDevices} أجهزة).";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(device.DeviceName))
        {
            ModelState.AddModelError("DeviceName", "اسم الجهاز مطلوب");
            ViewBag.Tenant = tenant;
            ViewBag.CurrentCount = currentCount;
            ViewBag.MaxDevices = tenant.MaxDevices;
            return View(device);
        }

        int portCount = device.DeviceType switch
        {
            DeviceType.GoIP1 => 1,
            DeviceType.GoIP4 => 4,
            DeviceType.GoIP8 => 8,
            DeviceType.GoIP16 => 16,
            DeviceType.GoIP32 => 32,
            _ => 128
        };

        device.TotalPorts = portCount;
        device.ActivePorts = 0;
        device.IsActive = true;
        device.CreatedDate = DateTime.UtcNow;

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        // 1. Create Ports
        var ports = new List<DevicePort>();
        for (int i = 1; i <= portCount; i++)
        {
            ports.Add(new DevicePort
            {
                DeviceId = device.Id,
                PortNumber = i,
                PortLabel = $"L{i:D3}",
                Status = PortStatus.Empty,
                IsActive = true
            });
        }
        _context.DevicePorts.AddRange(ports);

        // 2. Map Device to Tenant
        var td = new TenantDevice
        {
            TenantId = tenantId.Value,
            DeviceId = device.Id,
            PortRangeStart = 1,
            PortRangeEnd = portCount,
            IsActive = true,
            AssignedDate = DateTime.UtcNow
        };
        _context.TenantDevices.Add(td);

        await _context.SaveChangesAsync();

        TempData["Success"] = $"تمت إضافة جهاز '{device.DeviceName}' وتوليد {portCount} منفذ بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var td = await _context.TenantDevices
            .Include(t => t.Device)
                .ThenInclude(d => d.Ports.OrderBy(p => p.PortNumber))
                    .ThenInclude(p => p.SimCard)
            .FirstOrDefaultAsync(t => t.DeviceId == id && t.TenantId == tenantId.Value && !t.Device.IsDelete);

        if (td == null) return NotFound();
        return View(td.Device);
    }
}
