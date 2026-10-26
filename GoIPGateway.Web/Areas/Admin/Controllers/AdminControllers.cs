using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Features.Devices.Commands;
using GoIPGateway.Application.Features.SimCards.Commands;
using GoIPGateway.Application.Features.Tenants.Commands;
using GoIPGateway.Application.Features.Transactions.Commands;
using GoIPGateway.Application.Features.Transactions.Queries;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GoIPGateway.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class DashboardController : Controller
{
    private readonly IAppDbContext _context;

    public DashboardController(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;

        var totalTenants = await _context.Tenants.CountAsync(t => !t.IsDelete);
        var devices = await _context.Devices
            .Include(d => d.Ports)
            .Where(d => !d.IsDelete)
            .ToListAsync();

        var totalSims = await _context.SimCards.CountAsync(s => !s.IsDelete);
        var activeSims = await _context.SimCards.CountAsync(s => s.IsActive && !s.IsDelete && !s.IsBlocked);
        var totalBalance = await _context.SimCards.Where(s => !s.IsDelete).SumAsync(s => s.CurrentBalance);

        var todayTxs = await _context.TenantTransactions
            .Where(t => t.RequestedAt >= today)
            .ToListAsync();

        var recentTxs = await _context.TenantTransactions
            .Include(t => t.Tenant)
            .Include(t => t.ServiceDefinition)
            .Include(t => t.SimCard)
            .OrderByDescending(t => t.RequestedAt)
            .Take(10)
            .ToListAsync();

        var lowSims = await _context.SimCards
            .Include(s => s.DevicePort)
            .Where(s => s.CurrentBalance < 1000 && !s.IsDelete)
            .OrderBy(s => s.CurrentBalance)
            .Take(5)
            .ToListAsync();

        var vm = new AdminDashboardViewModel
        {
            TotalTenants = totalTenants,
            TotalDevices = devices.Count,
            OnlineDevices = devices.Count(d => d.IsOnline),
            TotalSimCards = totalSims,
            ActiveSimCards = activeSims,
            TodayTransactionsCount = todayTxs.Count,
            TodaySuccessfulTransactions = todayTxs.Count(t => t.Status == TransactionStatus.Completed),
            TodayFailedTransactions = todayTxs.Count(t => t.Status == TransactionStatus.Failed),
            TotalSimBalance = totalBalance,
            Devices = devices,
            RecentTransactions = recentTxs,
            LowBalanceSims = lowSims
        };

        return View(vm);
    }
}

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IMediator _mediator;

    public TenantsController(IAppDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var tenants = await _context.Tenants
            .Include(t => t.ApiKeys)
            .Include(t => t.AssignedDevices)
            .Include(t => t.SubscribedServices)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

        return View(tenants);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateTenantCommand("", "", "", "", "", ""));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTenantCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
        {
            TempData["Success"] = "تمت إضافة الشركة بنجاح وإنشاء مفتاح API افتراضي.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error.Message);
        return View(command);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id && !t.IsDelete);
        if (tenant == null) return NotFound();
        return View(tenant);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Tenant model)
    {
        if (id != model.Id) return BadRequest();

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id && !t.IsDelete);
        if (tenant == null) return NotFound();

        if (string.IsNullOrWhiteSpace(model.CompanyName))
        {
            ModelState.AddModelError("CompanyName", "اسم الشركة بالعربية مطلوب");
            return View(model);
        }

        tenant.CompanyName = model.CompanyName.Trim();
        tenant.CompanyNameEn = model.CompanyNameEn?.Trim();
        tenant.ContactPerson = model.ContactPerson?.Trim();
        tenant.ContactEmail = model.ContactEmail?.Trim();
        tenant.ContactPhone = model.ContactPhone?.Trim();
        tenant.Address = model.Address?.Trim();
        tenant.TaxId = model.TaxId?.Trim();
        tenant.MaxDevices = model.MaxDevices > 0 ? model.MaxDevices : 1;
        tenant.MaxDailyOperations = model.MaxDailyOperations > 0 ? model.MaxDailyOperations : 1000;
        tenant.MaxMonthlyOps = model.MaxMonthlyOps > 0 ? model.MaxMonthlyOps : 30000;
        tenant.CreditLimit = model.CreditLimit;
        tenant.IsActive = model.IsActive;
        tenant.IsBlocked = model.IsBlocked;
        tenant.BlockReason = model.IsBlocked ? model.BlockReason?.Trim() : null;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"تم تحديث بيانات شركة '{tenant.CompanyName}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var tenant = await _context.Tenants
            .Include(t => t.ApiKeys)
            .Include(t => t.AssignedDevices)
                .ThenInclude(td => td.Device)
            .Include(t => t.SubscribedServices)
                .ThenInclude(ts => ts.ServiceDefinition)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDelete);

        if (tenant == null) return NotFound();
        return View(tenant);
    }

    [HttpGet]
    public async Task<IActionResult> ManageServices(int id)
    {
        var tenant = await _context.Tenants
            .Include(t => t.SubscribedServices)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDelete);

        if (tenant == null) return NotFound();

        var allServices = await _context.ServiceDefinitions
            .Where(s => s.IsActive && !s.IsDelete)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SortOrder)
            .ToListAsync();

        ViewBag.Tenant = tenant;
        ViewBag.SubscribedIds = tenant.SubscribedServices.Where(s => s.IsActive).Select(s => s.ServiceDefinitionId).ToList();
        return View(allServices);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveServices(int tenantId, int[]? selectedServiceIds)
    {
        var tenant = await _context.Tenants
            .Include(t => t.SubscribedServices)
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDelete);

        if (tenant == null) return NotFound();

        var selected = selectedServiceIds ?? Array.Empty<int>();

        // Remove unselected
        var toRemove = tenant.SubscribedServices.Where(ts => !selected.Contains(ts.ServiceDefinitionId)).ToList();
        foreach (var r in toRemove)
        {
            _context.TenantServices.Remove(r);
        }

        // Add new
        var existingIds = tenant.SubscribedServices.Select(ts => ts.ServiceDefinitionId).ToList();
        foreach (var sId in selected)
        {
            if (!existingIds.Contains(sId))
            {
                _context.TenantServices.Add(new TenantService
                {
                    TenantId = tenantId,
                    ServiceDefinitionId = sId,
                    IsActive = true,
                    AssignedDate = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"تم حفظ وتحديث الخدمات المتاحة لشركة '{tenant.CompanyName}' بنجاح.";
        return RedirectToAction(nameof(ManageServices), new { id = tenantId });
    }

    [HttpGet]
    public async Task<IActionResult> ManageTokens(int id)
    {
        var tenant = await _context.Tenants
            .Include(t => t.ApiKeys)
            .Include(t => t.SubscribedServices)
                .ThenInclude(ts => ts.ServiceDefinition)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDelete);

        if (tenant == null) return NotFound();

        ViewBag.Tenant = tenant;
        return View(tenant);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateToken(int tenantId, string keyName, int[]? allowedServiceIds, int rateLimit = 100, int expiryMonths = 12)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return NotFound();

        var apiKeyStr = "gw_" + Guid.NewGuid().ToString("N")[..16];
        var secretKeyStr = "sec_" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        string? allowedJson = null;
        if (allowedServiceIds != null && allowedServiceIds.Any())
        {
            allowedJson = JsonSerializer.Serialize(allowedServiceIds);
        }

        var key = new TenantApiKey
        {
            TenantId = tenantId,
            KeyName = string.IsNullOrWhiteSpace(keyName) ? "API Token" : keyName.Trim(),
            ApiKey = apiKeyStr,
            SecretKey = secretKeyStr,
            AllowedServices = allowedJson,
            RateLimit = rateLimit > 0 ? rateLimit : 100,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMonths(expiryMonths > 0 ? expiryMonths : 12)
        };

        _context.TenantApiKeys.Add(key);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"تم إنشاء التوكن '{key.KeyName}' بنجاح مع تحديد الصلاحيات للخدمات المحددة.";
        return RedirectToAction(nameof(ManageTokens), new { id = tenantId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeToken(int id, int tenantId)
    {
        var key = await _context.TenantApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId);
        if (key != null)
        {
            key.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تعطيل التوكن بنجاح.";
        }
        return RedirectToAction(nameof(ManageTokens), new { id = tenantId });
    }

    [HttpPost]
    public async Task<IActionResult> GenerateApiKey(int tenantId, string keyName)
    {
        var result = await _mediator.Send(new GenerateApiKeyCommand(tenantId, keyName));
        if (result.IsSuccess)
        {
            TempData["Success"] = $"تم إنشاء المفتاح بنجاح: {result.Value.ApiKey}";
        }
        else
        {
            TempData["Error"] = result.Error.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class DevicesController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IMediator _mediator;

    public DevicesController(IAppDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var devices = await _context.Devices
            .Include(d => d.Ports)
                .ThenInclude(p => p.SimCard)
            .ToListAsync();

        return View(devices);
    }

    public async Task<IActionResult> Details(int id)
    {
        var device = await _context.Devices
            .Include(d => d.Ports.OrderBy(p => p.PortNumber))
                .ThenInclude(p => p.SimCard)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null) return NotFound();
        return View(device);
    }

    [HttpGet]
    public IActionResult Create() => View(new RegisterDeviceCommand("", DeviceType.GoIP128, "192.168.1.100", 80, "admin", "admin"));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterDeviceCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
        {
            TempData["Success"] = "تم تسجيل جهاز GoIP وتوليد البورتات بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error.Message);
        return View(command);
    }
}

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class SimCardsController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IMediator _mediator;

    public SimCardsController(IAppDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(OperatorType? op)
    {
        var query = _context.SimCards
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .Where(s => !s.IsDelete);

        if (op.HasValue)
        {
            query = query.Where(s => s.Operator == op.Value);
        }

        var sims = await query.OrderBy(s => s.DevicePort.PortNumber).ToListAsync();
        ViewBag.SelectedOperator = op;
        return View(sims);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var assignedPortIds = await _context.SimCards
            .Where(s => !s.IsDelete)
            .Select(s => s.DevicePortId)
            .ToListAsync();

        var availablePorts = await _context.DevicePorts
            .Include(p => p.Device)
            .Where(p => !assignedPortIds.Contains(p.Id) && p.IsActive)
            .OrderBy(p => p.DeviceId).ThenBy(p => p.PortNumber)
            .ToListAsync();

        ViewBag.AvailablePorts = availablePorts;
        return View(new SimCard { DailyLimit = 50000, MonthlyLimit = 500000, IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SimCard model)
    {
        if (model.DevicePortId <= 0)
        {
            ModelState.AddModelError("DevicePortId", "يرجى اختيار البورت والجهاز المراد تركيب الشريحة به.");
        }

        if (string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            ModelState.AddModelError("PhoneNumber", "يرجى إدخال رقم هاتف الشريحة.");
        }

        if (!ModelState.IsValid)
        {
            var assignedPortIds = await _context.SimCards.Where(s => !s.IsDelete).Select(s => s.DevicePortId).ToListAsync();
            ViewBag.AvailablePorts = await _context.DevicePorts.Include(p => p.Device).Where(p => !assignedPortIds.Contains(p.Id) && p.IsActive).ToListAsync();
            return View(model);
        }

        model.SimNumber = model.PhoneNumber;
        model.CreatedDate = DateTime.UtcNow;
        model.IsActive = true;

        _context.SimCards.Add(model);

        var port = await _context.DevicePorts.FindAsync(model.DevicePortId);
        if (port != null)
        {
            port.Status = PortStatus.Active;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"تمت إضافة الشريحة {model.PhoneNumber} وربطها بالبورت بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var sim = await _context.SimCards
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete);

        if (sim == null) return NotFound();
        return View(sim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SimCard model)
    {
        var sim = await _context.SimCards.FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete);
        if (sim == null) return NotFound();

        sim.PhoneNumber = model.PhoneNumber;
        sim.SimNumber = model.PhoneNumber;
        sim.Operator = model.Operator;
        sim.WalletPassword = model.WalletPassword;
        sim.UssdPassword = model.UssdPassword;
        sim.CurrentBalance = model.CurrentBalance;
        sim.DailyLimit = model.DailyLimit;
        sim.MonthlyLimit = model.MonthlyLimit;
        sim.IsActive = model.IsActive;
        sim.IsBlocked = model.IsBlocked;
        sim.Notes = model.Notes;
        sim.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"تم حفظ تعديلات الشريحة {sim.PhoneNumber} بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var sim = await _context.SimCards.FindAsync(id);
        if (sim != null)
        {
            sim.IsActive = !sim.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"تم {(sim.IsActive ? "تفعيل" : "إيقاف")} الشريحة بنجاح.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var sim = await _context.SimCards.Include(s => s.DevicePort).FirstOrDefaultAsync(s => s.Id == id);
        if (sim != null)
        {
            sim.IsDelete = true;
            if (sim.DevicePort != null)
            {
                sim.DevicePort.Status = PortStatus.Empty;
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حذف الشريحة وتحرير البورت بنجاح.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> QueryBalance(int simId)
    {
        var command = new ExecuteServiceCommand("BAL_INQ", null, simId, null, new Dictionary<string, string>(), null, RequestSource.Dashboard);
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
            TempData["Success"] = $"تم استعلام الرصيد بنجاح: {result.Value.Message}";
        else
            TempData["Error"] = $"فشل استعلام الرصيد: {result.Error.Message}";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> QueryNumber(int simId)
    {
        var command = new ExecuteServiceCommand("NUM_INQ", null, simId, null, new Dictionary<string, string>(), null, RequestSource.Dashboard);
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
            TempData["Success"] = $"تم استعلام رقم الشريحة بنجاح: {result.Value.Message}";
        else
            TempData["Error"] = $"فشل استعلام رقم الشريحة: {result.Error.Message}";

        return RedirectToAction(nameof(Index));
    }
}

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class ServicesController : Controller
{
    private readonly IAppDbContext _context;

    public ServicesController(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var services = await _context.ServiceDefinitions
            .Include(s => s.Parameters.OrderBy(p => p.SortOrder))
            .Include(s => s.UssdSteps.Where(st => !st.IsDelete).OrderBy(st => st.StepOrder))
            .Where(s => !s.IsDelete)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return View(services);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ServiceDefinition
        {
            ServiceType = ServiceType.USSD,
            Category = ServiceCategory.Payment,
            BalanceActionType = BalanceActionType.Decrement,
            BaseCost = 0.5m,
            IsActive = true,
            SortOrder = 1,
            IconClass = "bi-lightning-charge"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceDefinition model)
    {
        model.ServiceCode = model.ServiceCode?.Trim().ToUpperInvariant() ?? "";
        model.NameAr = model.NameAr?.Trim() ?? "";
        model.NameEn = string.IsNullOrWhiteSpace(model.NameEn) ? null : model.NameEn.Trim();
        model.ServiceType = ServiceType.USSD; // Force USSD execution only

        if (string.IsNullOrWhiteSpace(model.ServiceCode))
        {
            ModelState.AddModelError("ServiceCode", "كود الخدمة في الـ API مطلوب (مثال: VF_PAY).");
        }

        if (string.IsNullOrWhiteSpace(model.NameAr))
        {
            ModelState.AddModelError("NameAr", "اسم الخدمة بالعربية مطلوب.");
        }

        if (!model.Operator.HasValue)
        {
            ModelState.AddModelError("Operator", "يرجى تحديد شركة المحمول (فودافون، أورانج، اتصالات، وي).");
        }

        // Check if this ServiceCode already exists in DB (including soft-deleted rows)
        var existingByCode = await _context.ServiceDefinitions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.ServiceCode.ToLower() == model.ServiceCode.ToLower());

        if (existingByCode != null)
        {
            if (existingByCode.IsDelete)
            {
                // Auto-restore and overwrite the previously soft-deleted service with the new settings
                existingByCode.IsDelete = false;
                existingByCode.NameAr = model.NameAr;
                existingByCode.NameEn = model.NameEn;
                existingByCode.Description = model.Description;
                existingByCode.Category = model.Category;
                existingByCode.BalanceActionType = model.BalanceActionType;
                existingByCode.ServiceType = ServiceType.USSD;
                existingByCode.BaseCost = model.BaseCost;
                existingByCode.Operator = model.Operator;
                existingByCode.IconClass = model.IconClass;
                existingByCode.SortOrder = model.SortOrder;
                existingByCode.IsActive = model.IsActive;
                existingByCode.ModifiedDate = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"تمت استعادة وتحديث الخدمة '{existingByCode.NameAr}' بنجاح.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("ServiceCode", $"كود الخدمة '{model.ServiceCode}' مسجل مسبقاً في قاعدة البيانات.");
                    TempData["Error"] = $"كود الخدمة '{model.ServiceCode}' مسجل مسبقاً في قاعدة البيانات.";
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("ServiceCode", $"كود الخدمة '{model.ServiceCode}' مسجل بالفعل لخدمة '{existingByCode.NameAr}'، يرجى اختيار كود آخر.");
            }
        }

        if (await _context.ServiceDefinitions.AnyAsync(s => s.NameAr.ToLower() == model.NameAr.ToLower() && !s.IsDelete))
        {
            ModelState.AddModelError("NameAr", $"اسم الخدمة بالعربية '{model.NameAr}' مسجل مسبقاً، يرجى كتابة اسم غير مكرر.");
        }

        if (!string.IsNullOrEmpty(model.NameEn) && await _context.ServiceDefinitions.AnyAsync(s => s.NameEn != null && s.NameEn.ToLower() == model.NameEn.ToLower() && !s.IsDelete))
        {
            ModelState.AddModelError("NameEn", $"اسم الخدمة بالإنجليزية '{model.NameEn}' مسجل مسبقاً، يرجى كتابة اسم غير مكرر.");
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "يرجى التحقق من صحة البيانات المدخلة.";
            TempData["Error"] = firstError;
            return View(model);
        }

        model.CreatedDate = DateTime.UtcNow;
        _context.ServiceDefinitions.Add(model);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = $"تمت إضافة الخدمة '{model.NameAr}' بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("ServiceCode", $"تعذر إضافة الخدمة: كود الخدمة '{model.ServiceCode}' مسجل مسبقاً في قاعدة البيانات، يرجى اختيار كود آخر.");
            TempData["Error"] = $"كود الخدمة '{model.ServiceCode}' مسجل مسبقاً في قاعدة البيانات.";
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var service = await _context.ServiceDefinitions
            .Include(s => s.Parameters.Where(p => p.IsActive).OrderBy(p => p.SortOrder))
            .Include(s => s.UssdSteps.Where(st => !st.IsDelete).OrderBy(st => st.StepOrder))
            .Include(s => s.TenantSubscriptions.Where(ts => !ts.IsDelete))
                .ThenInclude(ts => ts.Tenant)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete);

        if (service == null) return NotFound();
        return View(service);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var service = await _context.ServiceDefinitions.FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete);
        if (service == null) return NotFound();
        return View(service);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceDefinition model)
    {
        var service = await _context.ServiceDefinitions.FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete);
        if (service == null) return NotFound();

        model.ServiceCode = model.ServiceCode?.Trim().ToUpperInvariant() ?? "";
        model.NameAr = model.NameAr?.Trim() ?? "";
        model.NameEn = string.IsNullOrWhiteSpace(model.NameEn) ? null : model.NameEn.Trim();

        if (string.IsNullOrWhiteSpace(model.ServiceCode))
            ModelState.AddModelError("ServiceCode", "كود الخدمة مطلوب.");

        if (string.IsNullOrWhiteSpace(model.NameAr))
            ModelState.AddModelError("NameAr", "اسم الخدمة بالعربية مطلوب.");

        if (!model.Operator.HasValue)
            ModelState.AddModelError("Operator", "يرجى تحديد شركة المحمول.");

        if (await _context.ServiceDefinitions.IgnoreQueryFilters().AnyAsync(s => s.Id != id && s.ServiceCode.ToLower() == model.ServiceCode.ToLower()))
            ModelState.AddModelError("ServiceCode", $"كود الخدمة '{model.ServiceCode}' مستخدم بالفعل لخدمة أخرى.");

        if (await _context.ServiceDefinitions.AnyAsync(s => s.Id != id && s.NameAr.ToLower() == model.NameAr.ToLower() && !s.IsDelete))
            ModelState.AddModelError("NameAr", $"اسم الخدمة بالعربية '{model.NameAr}' مستخدم بالفعل لخدمة أخرى.");

        if (!string.IsNullOrEmpty(model.NameEn) && await _context.ServiceDefinitions.AnyAsync(s => s.Id != id && s.NameEn != null && s.NameEn.ToLower() == model.NameEn.ToLower() && !s.IsDelete))
            ModelState.AddModelError("NameEn", $"اسم الخدمة بالإنجليزية '{model.NameEn}' مستخدم بالفعل لخدمة أخرى.");

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "بيانات غير صالحة.";
            TempData["Error"] = firstError;
            return View(model);
        }

        service.ServiceCode = model.ServiceCode;
        service.NameAr = model.NameAr;
        service.NameEn = model.NameEn;
        service.Description = model.Description;
        service.Category = model.Category;
        service.BalanceActionType = model.BalanceActionType;
        service.ServiceType = ServiceType.USSD;
        service.BaseCost = model.BaseCost;
        service.Operator = model.Operator;
        service.IconClass = model.IconClass;
        service.SortOrder = model.SortOrder;
        service.IsActive = model.IsActive;
        service.ModifiedDate = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = $"تم حفظ تعديلات الخدمة '{service.NameAr}' بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("ServiceCode", $"تعذر التعديل: كود الخدمة '{model.ServiceCode}' مستخدم لخدمة أخرى.");
            TempData["Error"] = $"كود الخدمة '{model.ServiceCode}' مستخدم بالفعل في قاعدة البيانات.";
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var service = await _context.ServiceDefinitions.FindAsync(id);
        if (service == null) return NotFound();

        service.IsActive = !service.IsActive;
        service.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"تم {(service.IsActive ? "تفعيل" : "إيقاف")} الخدمة '{service.NameAr}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _context.ServiceDefinitions.FindAsync(id);
        if (service == null) return NotFound();

        service.IsDelete = true;
        service.IsActive = false;
        service.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"تم حذف الخدمة '{service.NameAr}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ConfigureSteps(int id)
    {
        var service = await _context.ServiceDefinitions
            .Include(s => s.Parameters.OrderBy(p => p.SortOrder))
            .Include(s => s.UssdSteps.Where(st => !st.IsDelete).OrderBy(st => st.StepOrder))
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete);

        if (service == null) return NotFound();
        return View(service);
    }

    [HttpGet]
    public async Task<IActionResult> CreateStep(int serviceId)
    {
        var service = await _context.ServiceDefinitions
            .Include(s => s.UssdSteps.Where(st => !st.IsDelete))
            .FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDelete);

        if (service == null) return NotFound();

        var nextOrder = service.UssdSteps.Any() ? service.UssdSteps.Max(s => s.StepOrder) + 1 : 1;
        var step = new ServiceUssdStep
        {
            ServiceDefinitionId = serviceId,
            ServiceDefinition = service,
            StepOrder = nextOrder,
            TimeoutSeconds = 30,
            DelayAfterMs = 1000
        };

        ViewBag.Service = service;
        return View(step);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStep(
        int serviceId, 
        ServiceUssdStep step, 
        string[]? successPatterns, 
        string[]? successClientMessages, 
        string[]? failurePatterns, 
        string[]? failureClientMessages)
    {
        var service = await _context.ServiceDefinitions.FindAsync(serviceId);
        if (service == null) return NotFound();

        step.ServiceDefinitionId = serviceId;
        step.CreatedDate = DateTime.UtcNow;
        step.IsActive = true;

        PackStepRules(step, successPatterns, successClientMessages, failurePatterns, failureClientMessages);

        _context.ServiceUssdSteps.Add(step);
        await _context.SaveChangesAsync();

        TempData["Success"] = "تمت إضافة خطوة USSD جديدة بنجاح.";
        return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
    }

    [HttpGet]
    public async Task<IActionResult> EditStep(int id)
    {
        var step = await _context.ServiceUssdSteps
            .Include(s => s.ServiceDefinition)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete);

        if (step == null) return NotFound();

        ViewBag.Service = step.ServiceDefinition;
        return View(step);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStep(
        int id, 
        int serviceId, 
        ServiceUssdStep model, 
        string[]? successPatterns, 
        string[]? successClientMessages, 
        string[]? failurePatterns, 
        string[]? failureClientMessages)
    {
        var step = await _context.ServiceUssdSteps.FirstOrDefaultAsync(s => s.Id == id && s.ServiceDefinitionId == serviceId);
        if (step == null) return NotFound();

        step.StepTitle = model.StepTitle;
        step.StepOrder = model.StepOrder;
        step.UssdCommand = model.UssdCommand;
        step.TimeoutSeconds = model.TimeoutSeconds;
        step.DelayAfterMs = model.DelayAfterMs;
        step.ModifiedDate = DateTime.UtcNow;

        PackStepRules(step, successPatterns, successClientMessages, failurePatterns, failureClientMessages);

        await _context.SaveChangesAsync();
        TempData["Success"] = "تم تعديل خطوة USSD بنجاح.";
        return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
    }

    [HttpGet]
    public async Task<IActionResult> StepDetails(int id)
    {
        var step = await _context.ServiceUssdSteps
            .Include(s => s.ServiceDefinition)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete);

        if (step == null) return NotFound();

        ViewBag.Service = step.ServiceDefinition;
        return View(step);
    }

    private static void PackStepRules(
        ServiceUssdStep target, 
        string[]? successPatterns, 
        string[]? successClientMessages, 
        string[]? failurePatterns, 
        string[]? failureClientMessages)
    {
        // 1. Pack Success Rules (Pairs: Pattern + ClientMessage)
        var successRules = new List<UssdRuleItem>();
        if (successPatterns != null)
        {
            for (int i = 0; i < successPatterns.Length; i++)
            {
                var pat = successPatterns[i]?.Trim();
                if (!string.IsNullOrWhiteSpace(pat))
                {
                    var msg = (successClientMessages != null && i < successClientMessages.Length) 
                        ? successClientMessages[i]?.Trim() 
                        : null;
                    successRules.Add(new UssdRuleItem
                    {
                        Pattern = pat,
                        ClientMessage = string.IsNullOrWhiteSpace(msg) ? null : msg
                    });
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(target.SuccessPattern) && !target.SuccessPattern.TrimStart().StartsWith("["))
        {
            successRules.Add(new UssdRuleItem { Pattern = target.SuccessPattern.Trim() });
        }

        target.SuccessPattern = successRules.Any() ? JsonSerializer.Serialize(successRules) : null;

        // 2. Pack Failure Rules (Pairs: Pattern + Safe ClientMessage)
        var failureRules = new List<UssdRuleItem>();
        if (failurePatterns != null)
        {
            for (int i = 0; i < failurePatterns.Length; i++)
            {
                var pat = failurePatterns[i]?.Trim();
                if (!string.IsNullOrWhiteSpace(pat))
                {
                    var msg = (failureClientMessages != null && i < failureClientMessages.Length) 
                        ? failureClientMessages[i]?.Trim() 
                        : null;
                    failureRules.Add(new UssdRuleItem
                    {
                        Pattern = pat,
                        ClientMessage = string.IsNullOrWhiteSpace(msg) ? null : msg
                    });
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(target.FailurePattern) && !target.FailurePattern.TrimStart().StartsWith("["))
        {
            failureRules.Add(new UssdRuleItem { Pattern = target.FailurePattern.Trim() });
        }

        target.FailurePattern = failureRules.Any() ? JsonSerializer.Serialize(failureRules) : null;
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStep(int id, int serviceId)
    {
        var step = await _context.ServiceUssdSteps.FirstOrDefaultAsync(s => s.Id == id && s.ServiceDefinitionId == serviceId);
        if (step != null)
        {
            step.IsDelete = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حذف خطوة الـ USSD بنجاح.";
        }
        return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddParameter(int serviceId, ServiceParameter param)
    {
        param.ServiceDefinitionId = serviceId;
        param.IsActive = true;
        _context.ServiceParameters.Add(param);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"تمت إضافة البارامتر '{param.DisplayNameAr}' بنجاح.";
        return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteParameter(int id, int serviceId)
    {
        var param = await _context.ServiceParameters.FirstOrDefaultAsync(p => p.Id == id && p.ServiceDefinitionId == serviceId);
        if (param != null)
        {
            _context.ServiceParameters.Remove(param);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حذف البارامتر بنجاح.";
        }
        return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
    }
}


[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class TransactionsController : Controller
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(int? simCardId, TransactionStatus? status, int page = 1)
    {
        var result = await _mediator.Send(new GetTransactionsListQuery(
            SimCardId: simCardId,
            Status: status,
            PageNumber: page,
            PageSize: 20
        ));

        return View(result.Value);
    }

    public async Task<IActionResult> Details(string refCode)
    {
        var result = await _mediator.Send(new GetTransactionByRefQuery(refCode));
        if (result.IsFailure) return NotFound();
        return PartialView("_TransactionDetailsModal", result.Value);
    }
}
