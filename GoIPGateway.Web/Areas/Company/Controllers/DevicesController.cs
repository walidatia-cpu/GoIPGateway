using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Features.Devices.Commands;
using GoIPGateway.Application.Features.Devices.Queries;
using GoIPGateway.Application.Features.Tenants.Queries;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator")]
public class DevicesController : Controller
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public DevicesController(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider)
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

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var tenantResult = await _mediator.Send(new GetTenantByIdQuery(tenantId.Value), cancellationToken);
        if (!tenantResult.Succeeded || tenantResult.Data == null) return NotFound();

        var tenant = tenantResult.Data;
        var assignmentsResult = await _mediator.Send(new GetTenantDeviceAssignmentsQuery(tenantId.Value), cancellationToken);
        var tenantDevices = assignmentsResult.Succeeded ? assignmentsResult.Data ?? new List<TenantDevice>() : new List<TenantDevice>();

        ViewBag.Tenant = tenant;
        ViewBag.CurrentCount = tenantDevices.Count;
        ViewBag.MaxDevices = tenant.MaxDevices;
        ViewBag.CanAddMore = tenantDevices.Count < tenant.MaxDevices;

        return View(tenantDevices);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var tenantResult = await _mediator.Send(new GetTenantByIdQuery(tenantId.Value), cancellationToken);
        if (!tenantResult.Succeeded || tenantResult.Data == null) return NotFound();

        var tenant = tenantResult.Data;
        var assignmentsResult = await _mediator.Send(new GetTenantDeviceAssignmentsQuery(tenantId.Value), cancellationToken);
        var currentCount = assignmentsResult.Succeeded ? assignmentsResult.Data?.Count ?? 0 : 0;

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
    public async Task<IActionResult> Create(Device device, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var tenantResult = await _mediator.Send(new GetTenantByIdQuery(tenantId.Value), cancellationToken);
        if (!tenantResult.Succeeded || tenantResult.Data == null) return NotFound();

        var tenant = tenantResult.Data;
        var assignmentsResult = await _mediator.Send(new GetTenantDeviceAssignmentsQuery(tenantId.Value), cancellationToken);
        var currentCount = assignmentsResult.Succeeded ? assignmentsResult.Data?.Count ?? 0 : 0;

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

        var registerCommand = new RegisterDeviceCommand(
            device.DeviceName,
            device.DeviceType,
            device.IpAddress,
            device.Port,
            device.Username,
            device.Password,
            device.Location,
            device.Notes);

        var registerResult = await _mediator.Send(registerCommand, cancellationToken);
        if (!registerResult.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, registerResult.Error?.Message ?? "فشلت عملية تسجيل الجهاز.");
            ViewBag.Tenant = tenant;
            ViewBag.CurrentCount = currentCount;
            ViewBag.MaxDevices = tenant.MaxDevices;
            return View(device);
        }

        var deviceId = registerResult.Value;
        var assignCommand = new AssignDeviceToTenantCommand(deviceId, tenantId.Value, 1, portCount);
        await _mediator.Send(assignCommand, cancellationToken);

        TempData["Success"] = $"تمت إضافة جهاز '{device.DeviceName}' وتوليد {portCount} منفذ بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var deviceResult = await _mediator.Send(new GetDeviceByIdQuery(id), cancellationToken);
        if (!deviceResult.Succeeded || deviceResult.Data == null) return NotFound();

        return View(deviceResult.Data);
    }
}
