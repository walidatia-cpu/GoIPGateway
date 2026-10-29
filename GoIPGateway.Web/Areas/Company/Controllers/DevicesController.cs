using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Features.Devices.Commands;
using GoIPGateway.Application.Features.Devices.Queries;
using GoIPGateway.Application.Features.Tenants.Queries;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using GoIPGateway.Application.Services.Devices;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator,SuperAdmin")]
public class DevicesController : Controller
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDeviceCommandService _deviceCommandService;

    public DevicesController(
        IMediator mediator,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        IDeviceCommandService deviceCommandService)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _deviceCommandService = deviceCommandService;
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
        var assignmentsResult = await _mediator.Send(new GetTenantDeviceAssignmentsQuery(tenantId.Value, IncludeDeleted: true), cancellationToken);
        var tenantDevices = assignmentsResult.Succeeded ? assignmentsResult.Data ?? new List<TenantDevice>() : new List<TenantDevice>();

        var activeCount = tenantDevices.Count(td => td.Device != null && !td.Device.IsDelete);
        var deletedCount = tenantDevices.Count(td => td.Device != null && td.Device.IsDelete);

        ViewBag.Tenant = tenant;
        ViewBag.CurrentCount = activeCount;
        ViewBag.DeletedCount = deletedCount;
        ViewBag.MaxDevices = tenant.MaxDevices;
        ViewBag.CanAddMore = activeCount < tenant.MaxDevices;

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
            DeviceType = DeviceType.GoIP128,
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

        int portCount = device.DeviceType == DeviceType.GoIP512 ? 512 : 128;

        var registerCommand = new RegisterDeviceCommand(
            device.DeviceName,
            device.DeviceType,
            device.IpAddress,
            device.Port,
            device.Username,
            device.Password,
            device.Location,
            device.Notes,
            device.MerchantCode?.Trim());

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

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var assignmentsResult = await _mediator.Send(new GetTenantDeviceAssignmentsQuery(tenantId.Value, IncludeDeleted: true), cancellationToken);
        var assignment = assignmentsResult.Data?.FirstOrDefault(td => td.DeviceId == id);
        if (assignment == null)
            return NotFound("الجهاز غير مخصص لشركتك أو غير موجود.");

        var deviceResult = await _mediator.Send(new GetDeviceByIdQuery(id), cancellationToken);
        if (!deviceResult.Succeeded || deviceResult.Data == null)
            return NotFound();

        return View(deviceResult.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Device model, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var assignmentsResult = await _mediator.Send(new GetTenantDeviceAssignmentsQuery(tenantId.Value, IncludeDeleted: true), cancellationToken);
        var assignment = assignmentsResult.Data?.FirstOrDefault(td => td.DeviceId == id);
        if (assignment == null)
            return NotFound("الجهاز غير مخصص لشركتك أو غير موجود.");

        if (string.IsNullOrWhiteSpace(model.DeviceName))
        {
            ModelState.AddModelError("DeviceName", "اسم الجهاز مطلوب.");
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.IpAddress))
        {
            ModelState.AddModelError("IpAddress", "عنوان IP مطلوب.");
            return View(model);
        }

        var updateCommand = new UpdateDeviceCommand(
            id,
            model.DeviceName.Trim(),
            model.IpAddress.Trim(),
            model.Port > 0 ? model.Port : 80,
            string.IsNullOrWhiteSpace(model.Username) ? "admin" : model.Username.Trim(),
            model.Password?.Trim() ?? string.Empty,
            model.Location?.Trim(),
            model.Notes?.Trim(),
            model.IsActive,
            model.MerchantCode?.Trim()
        );

        var result = await _mediator.Send(updateCommand, cancellationToken);
        if (result.IsSuccess)
        {
            TempData["Success"] = $"تم حفظ تعديلات جهاز '{model.DeviceName}' بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error?.Message ?? "فشل حفظ التعديلات.");
        TempData["Error"] = result.Error?.Message ?? "فشل حفظ التعديلات.";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var assignmentsResult = await _mediator.Send(new GetTenantDeviceAssignmentsQuery(tenantId.Value, IncludeDeleted: true), cancellationToken);
        var assignment = assignmentsResult.Data?.FirstOrDefault(td => td.DeviceId == id);
        if (assignment == null)
            return NotFound("الجهاز غير مخصص لشركتك.");

        var result = await _mediator.Send(new DeleteDeviceCommand(id), cancellationToken);
        if (result.IsSuccess)
        {
            TempData["Success"] = $"تم نقل جهاز '{assignment.Device?.DeviceName}' إلى سلة المحذوفات بنجاح (تعطيل مؤقت، يمكن استعادته في أي وقت).";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل حذف الجهاز.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var assignmentsResult = await _mediator.Send(new GetTenantDeviceAssignmentsQuery(tenantId.Value, IncludeDeleted: true), cancellationToken);
        var assignment = assignmentsResult.Data?.FirstOrDefault(td => td.DeviceId == id);
        if (assignment == null)
            return NotFound("الجهاز غير مخصص لشركتك.");

        var result = await _mediator.Send(new RestoreDeviceCommand(id), cancellationToken);
        if (result.IsSuccess)
        {
            TempData["Success"] = $"تمت استعادة جهاز '{assignment.Device?.DeviceName}' بنجاح وإعادته للعمل.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل استعادة الجهاز.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SwitchActiveGroupAjax([FromBody] SwitchGroupRequest req, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Json(new { success = false, message = "غير مصرح" });

        var assignmentsResult = await _mediator.Send(new GetTenantDeviceAssignmentsQuery(tenantId.Value, IncludeDeleted: false), cancellationToken);
        var assignment = assignmentsResult.Data?.FirstOrDefault(td => td.DeviceId == req.DeviceId);
        if (assignment == null) return Json(new { success = false, message = "الجهاز غير موجود أو غير مخصص لشركتك." });

        var result = await _deviceCommandService.SwitchActiveGroupAsync(req.DeviceId, req.Group, cancellationToken);
        if (result.Succeeded)
        {
            var letter = req.Group.ToUpperInvariant() switch { "1" or "01" or "A" => "A", "2" or "02" or "B" => "B", "3" or "03" or "C" => "C", "4" or "04" or "D" => "D", _ => req.Group };
            var num = req.Group.ToUpperInvariant() switch { "A" or "1" or "01" => "01", "B" or "2" or "02" => "02", "C" or "3" or "03" => "03", "D" or "4" or "04" => "04", _ => req.Group };
            return Json(new { success = true, group = letter, groupNumber = num, message = $"تم تفعيل مجموعة {letter} ({num}) في كافة بلوكات الجهاز بنجاح (بورت واحد نشط لكل بلوك)." });
        }

        return Json(new { success = false, message = result.Error?.Message ?? "فشل تبديل المجموعة." });
    }

    [HttpPost]
    public async Task<IActionResult> SwitchBlockSlotAjax([FromBody] SwitchBlockSlotRequest req, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Json(new { success = false, message = "غير مصرح" });

        var assignmentsResult = await _mediator.Send(new GetTenantDeviceAssignmentsQuery(tenantId.Value, IncludeDeleted: false), cancellationToken);
        var assignment = assignmentsResult.Data?.FirstOrDefault(td => td.DeviceId == req.DeviceId);
        if (assignment == null) return Json(new { success = false, message = "الجهاز غير موجود أو غير مخصص لشركتك." });

        var result = await _deviceCommandService.SwitchBlockSlotAsync(req.DeviceId, req.PortId, cancellationToken);
        if (result.Succeeded)
        {
            return Json(new { success = true, portId = req.PortId, message = "تم تفعيل المنفذ في هذا البلوك بنجاح وتحويل باقي منافذ البلوك لوضع الاستعداد." });
        }

        return Json(new { success = false, message = result.Error?.Message ?? "فشل تفعيل المنفذ." });
    }
}

public class SwitchGroupRequest
{
    public int DeviceId { get; set; }
    public string Group { get; set; } = "A";
}

public class SwitchBlockSlotRequest
{
    public int DeviceId { get; set; }
    public int PortId { get; set; }
}
