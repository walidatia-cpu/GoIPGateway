using GoIPGateway.Application.Features.Devices.Queries;
using GoIPGateway.Application.Features.SimCards.Commands;
using GoIPGateway.Application.Features.SimCards.Queries;
using GoIPGateway.Application.Features.Transactions.Commands;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class SimCardsController : Controller
{
    private readonly IMediator _mediator;

    public SimCardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(OperatorType? op, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSimCardsListQuery(op), cancellationToken);
        var sims = result.Succeeded ? result.Data ?? new List<SimCard>() : new List<SimCard>();
        ViewBag.SelectedOperator = op;
        return View(sims);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var portsResult = await _mediator.Send(new GetAvailableDevicePortsQuery(), cancellationToken);
        ViewBag.AvailablePorts = portsResult.Succeeded ? portsResult.Data ?? new List<DevicePort>() : new List<DevicePort>();

        return View(new SimCard { DailyLimit = 50000, MonthlyLimit = 500000, IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SimCard model, CancellationToken cancellationToken)
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
            var portsResult = await _mediator.Send(new GetAvailableDevicePortsQuery(), cancellationToken);
            ViewBag.AvailablePorts = portsResult.Succeeded ? portsResult.Data ?? new List<DevicePort>() : new List<DevicePort>();
            return View(model);
        }

        var command = new RegisterSimCardCommand(
            model.DevicePortId,
            model.Operator,
            model.PhoneNumber,
            model.PhoneNumber,
            null,
            model.WalletPassword,
            model.UssdPassword,
            model.DailyLimit > 0 ? model.DailyLimit : 50000,
            model.MonthlyLimit > 0 ? model.MonthlyLimit : 500000,
            model.CurrentBalance,
            model.Notes);

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            TempData["Success"] = $"تمت إضافة الشريحة {model.PhoneNumber} وربطها بالبورت بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error?.Message ?? "فشلت إضافة الشريحة.");
        var portsRetry = await _mediator.Send(new GetAvailableDevicePortsQuery(), cancellationToken);
        ViewBag.AvailablePorts = portsRetry.Succeeded ? portsRetry.Data ?? new List<DevicePort>() : new List<DevicePort>();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSimCardByIdQuery(id), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SimCard model, CancellationToken cancellationToken)
    {
        var command = new UpdateSimCardCommand(
            id,
            model.PhoneNumber,
            model.PhoneNumber,
            null,
            model.Operator,
            model.WalletPassword,
            model.UssdPassword,
            model.DailyLimit,
            model.MonthlyLimit,
            model.IsActive,
            model.IsBlocked,
            model.CurrentBalance,
            model.Notes);

        var result = await _mediator.Send(command, cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = $"تم حفظ تعديلات الشريحة {model.PhoneNumber} بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error?.Message ?? "فشلت عملية حفظ التعديلات.");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ToggleSimCardStatusCommand(id), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = $"تم تغيير حالة الشريحة بنجاح.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشلت العملية.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteSimCardCommand(id), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم حذف الشريحة وتحرير البورت بنجاح.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل حذف الشريحة.";
        }
        return RedirectToAction(nameof(Index));
    }
}