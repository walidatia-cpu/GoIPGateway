using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Features.Transactions.Commands;
using GoIPGateway.Application.Features.Transactions.Queries;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator,TenantViewer")]
public class DashboardController : Controller
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public DashboardController(IAppDbContext context, ICurrentUserService currentUser, ITenantProvider tenantProvider)
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
        if (!tenantId.HasValue)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            return RedirectToAction("AccessDenied", "Account", new { area = "" });
        }

        var tenant = await _context.Tenants
            .Include(t => t.ApiKeys)
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value);

        if (tenant == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            return RedirectToAction("AccessDenied", "Account", new { area = "" });
        }

        var today = DateTime.UtcNow.Date;
        var todayOps = await _context.TenantTransactions
            .Where(t => t.TenantId == tenant.Id && t.RequestedAt >= today)
            .ToListAsync();

        var recentTxs = await _context.TenantTransactions
            .Include(t => t.ServiceDefinition)
            .Include(t => t.SimCard)
            .Where(t => t.TenantId == tenant.Id)
            .OrderByDescending(t => t.RequestedAt)
            .Take(8)
            .ToListAsync();

        var assignedDeviceIds = await _context.TenantDevices
            .Where(td => td.TenantId == tenant.Id && td.IsActive)
            .Select(td => td.DeviceId)
            .ToListAsync();

        var sims = await _context.SimCards
            .Include(s => s.DevicePort)
            .Where(s => assignedDeviceIds.Contains(s.DevicePort.DeviceId) && s.IsActive && !s.IsDelete)
            .ToListAsync();

        var subscribedServices = await _context.TenantServices
            .Include(ts => ts.ServiceDefinition)
            .Where(ts => ts.TenantId == tenant.Id && ts.IsActive)
            .Select(ts => ts.ServiceDefinition)
            .ToListAsync();

        var vm = new CompanyDashboardViewModel
        {
            Tenant = tenant,
            TodayOperationsCount = todayOps.Count,
            TodaySuccessfulOps = todayOps.Count(t => t.Status == TransactionStatus.Completed),
            AssignedSimCards = sims,
            RecentTransactions = recentTxs,
            AvailableServices = subscribedServices,
            ApiKeys = tenant.ApiKeys.Where(k => k.IsActive).ToList()
        };

        return View(vm);
    }
}

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator")]
public class ExecuteController : Controller
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMediator _mediator;

    public ExecuteController(IAppDbContext context, ICurrentUserService currentUser, ITenantProvider tenantProvider, IMediator mediator)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _mediator = mediator;
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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue) return Forbid();

        var services = await _context.TenantServices
            .Include(ts => ts.ServiceDefinition)
                .ThenInclude(s => s.Parameters.Where(p => p.IsActive).OrderBy(p => p.SortOrder))
            .Where(ts => ts.TenantId == tenantId.Value && ts.IsActive)
            .Select(ts => ts.ServiceDefinition)
            .ToListAsync();

        var assignedDeviceIds = await _context.TenantDevices
            .Where(td => td.TenantId == tenantId.Value && td.IsActive)
            .Select(td => td.DeviceId)
            .ToListAsync();

        var sims = await _context.SimCards
            .Include(s => s.DevicePort)
            .Where(s => assignedDeviceIds.Contains(s.DevicePort.DeviceId) && s.IsActive && !s.IsDelete)
            .ToListAsync();

        var vm = new ExecuteServiceViewModel
        {
            Services = services,
            AvailableSims = sims
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(string serviceCode, int? simCardId, OperatorType? op, string? externalRef, [FromForm] IFormCollection form)
    {
        var parameters = new Dictionary<string, string>();
        foreach (var key in form.Keys)
        {
            if (key.StartsWith("param_"))
            {
                var realKey = key["param_".Length..];
                parameters[realKey] = form[key].ToString();
            }
        }

        var command = new ExecuteServiceCommand(
            ServiceCode: serviceCode,
            ExternalRef: externalRef,
            SimCardId: simCardId,
            Operator: op,
            Parameters: parameters,
            Source: RequestSource.Dashboard
        );

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
        {
            TempData["Success"] = $"تم تنفيذ العملية بنجاح! رقم المرجع: {result.Value.TransactionRef}. الاستجابة: {result.Value.Message ?? result.Value.RawResponse}";
        }
        else
        {
            TempData["Error"] = $"فشل تنفيذ العملية: {result.Error.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator,TenantViewer")]
public class TransactionsController : Controller
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAppDbContext _context;

    public TransactionsController(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider, IAppDbContext context)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _context = context;
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

    public async Task<IActionResult> Index(TransactionStatus? status, int page = 1)
    {
        var tenantId = await ResolveTenantIdAsync();

        var result = await _mediator.Send(new GetTransactionsListQuery(
            TenantId: tenantId,
            Status: status,
            PageNumber: page,
            PageSize: 20
        ));

        return View(result.Value);
    }
}

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator")]
public class ApiSettingsController : Controller
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public ApiSettingsController(IAppDbContext context, ICurrentUserService currentUser, ITenantProvider tenantProvider)
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

        var tenant = await _context.Tenants
            .Include(t => t.ApiKeys)
            .Include(t => t.SubscribedServices)
                .ThenInclude(ts => ts.ServiceDefinition)
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value);

        return View(tenant);
    }
}
