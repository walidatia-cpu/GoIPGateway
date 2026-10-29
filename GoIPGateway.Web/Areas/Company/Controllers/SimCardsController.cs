using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Features.Devices.Queries;
using GoIPGateway.Application.Features.SimCards.Commands;
using GoIPGateway.Application.Features.SimCards.Queries;
using GoIPGateway.Application.Features.Tenants.Queries;
using GoIPGateway.Application.Features.Transactions.Commands;
using GoIPGateway.Application.Features.Transactions.Queries;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator,SuperAdmin")]
public class SimCardsController : Controller
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public SimCardsController(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider)
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

        var result = await _mediator.Send(new GetCompanySimCardsQuery(tenantId.Value), cancellationToken);
        var sims = result.Succeeded ? result.Data ?? new List<SimCard>() : new List<SimCard>();

        ViewBag.TotalBalance = sims.Where(s => s.IsActive).Sum(s => s.CurrentBalance);
        ViewBag.ActiveCount = sims.Count(s => s.IsActive);
        ViewBag.CurrentTenantId = tenantId.Value;

        var devices = sims
            .Where(s => s.DevicePort?.Device != null)
            .Select(s => s.DevicePort!.Device!)
            .DistinctBy(d => d.Id)
            .ToList();
        ViewBag.Devices = devices;

        return View(sims);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? portId, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        var portsResult = await _mediator.Send(new GetCompanyEmptyPortsQuery(tenantId.Value), cancellationToken);
        var emptyPorts = portsResult.Succeeded ? portsResult.Data ?? new List<DevicePort>() : new List<DevicePort>();

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
    public async Task<IActionResult> Create(SimCard sim, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        if (sim.DevicePortId <= 0)
        {
            ModelState.AddModelError("DevicePortId", "المنفذ المختار غير صالح.");
        }

        if (string.IsNullOrWhiteSpace(sim.PhoneNumber))
        {
            ModelState.AddModelError("PhoneNumber", "يرجى إدخال رقم الهاتف.");
        }

        if (!ModelState.IsValid)
        {
            var portsResult = await _mediator.Send(new GetCompanyEmptyPortsQuery(tenantId.Value), cancellationToken);
            ViewBag.EmptyPorts = portsResult.Succeeded ? portsResult.Data ?? new List<DevicePort>() : new List<DevicePort>();
            return View(sim);
        }

        var command = new RegisterSimCardCommand(
            sim.DevicePortId,
            sim.Operator,
            sim.PhoneNumber,
            sim.PhoneNumber,
            null,
            sim.WalletPassword,
            sim.UssdPassword,
            sim.DailyLimit > 0 ? sim.DailyLimit : 50000,
            sim.MonthlyLimit > 0 ? sim.MonthlyLimit : 500000,
            sim.CurrentBalance,
            sim.Notes);

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            TempData["Success"] = $"تمت إضافة شريحة '{sim.PhoneNumber}' بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error?.Message ?? "فشلت إضافة الشريحة.");
        var retryPorts = await _mediator.Send(new GetCompanyEmptyPortsQuery(tenantId.Value), cancellationToken);
        ViewBag.EmptyPorts = retryPorts.Succeeded ? retryPorts.Data ?? new List<DevicePort>() : new List<DevicePort>();
        return View(sim);
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
            TempData["Success"] = $"تم تحديث بيانات شريحة '{model.PhoneNumber}' بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error?.Message ?? "فشل حفظ التعديلات.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteSimCardCommand(id), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تمت إزالة الشريحة بنجاح وأصبح المنفذ متاحاً.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل حذف الشريحة.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> QueryBalance(int simId, CancellationToken cancellationToken)
    {
        var command = new GoIPGateway.Application.Features.Transactions.Commands.ExecuteServiceCommand("BAL_INQ", null, simId, null, new Dictionary<string, string>(), null, RequestSource.Dashboard);
        var result = await _mediator.Send(command, cancellationToken);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (result.IsSuccess)
                return Json(new { success = true, message = result.Value.Message, balance = result.Value.SimBalanceAfter, rawResponse = result.Value.RawResponse });
            return Json(new { success = false, message = result.Error.Message });
        }

        if (result.IsSuccess)
            TempData["Success"] = $"تم استعلام الرصيد بنجاح: {result.Value.Message}";
        else
            TempData["Error"] = $"فشل استعلام الرصيد: {result.Error.Message}";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> QueryNumber(int simId, CancellationToken cancellationToken)
    {
        var command = new GoIPGateway.Application.Features.Transactions.Commands.ExecuteServiceCommand("NUM_INQ", null, simId, null, new Dictionary<string, string>(), null, RequestSource.Dashboard);
        var result = await _mediator.Send(command, cancellationToken);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (result.IsSuccess)
                return Json(new { success = true, message = result.Value.Message, rawResponse = result.Value.RawResponse });
            return Json(new { success = false, message = result.Error.Message });
        }

        if (result.IsSuccess)
            TempData["Success"] = $"تم استعلام رقم الشريحة بنجاح: {result.Value.Message}";
        else
            TempData["Error"] = $"فشل استعلام رقم الشريحة: {result.Error.Message}";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> BulkExecute([FromBody] BulkSimActionRequest request, [FromServices] IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        if (request == null || request.SimIds == null || !request.SimIds.Any())
        {
            return Json(new { success = false, message = "يرجى تحديد شريحة واحدة على الأقل لتنفيذ الإجراء الجماعي." });
        }

        var validSimIds = request.SimIds.Distinct().ToList();
        var actionCode = (request.ActionCode ?? "").Trim().ToUpperInvariant();

        if (actionCode != "BAL_INQ" && actionCode != "NUM_INQ")
        {
            return Json(new { success = false, message = "نوع الإجراء غير صالح (يجب أن يكون استعلام رصيد BAL_INQ أو استعلام رقم NUM_INQ)." });
        }

        var effectiveTenantId = tenantId.Value;

        // Launch background task to process requests and broadcast SignalR updates
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
            tenantProvider.SetCurrentTenantId(effectiveTenantId);

            foreach (var simId in validSimIds)
            {
                try
                {
                    var command = new ExecuteServiceCommand(
                        ServiceCode: actionCode,
                        ExternalRef: $"BULK-{DateTime.UtcNow:HHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                        SimCardId: simId,
                        Operator: null,
                        Parameters: new Dictionary<string, string>(),
                        CallbackUrl: null,
                        Source: RequestSource.Dashboard
                    );

                    await mediator.Send(command);
                    await Task.Delay(350); // slight delay between commands to avoid port collision
                }
                catch
                {
                    // logged by command handler
                }
            }
        });

        var actionName = actionCode == "BAL_INQ" ? "استعلام الرصيد" : "استعلام رقم الشريحة";
        return Json(new
        {
            success = true,
            count = validSimIds.Count,
            action = actionCode,
            message = $"تم إطلاق طلبات {actionName} لعدد {validSimIds.Count} شريحة بنجاح. تتابع العمليات وحالاتها لحظياً في الجدول أدناه عبر SignalR."
        });
    }
}

public class BulkSimActionRequest
{
    public List<int> SimIds { get; set; } = new();
    public string ActionCode { get; set; } = string.Empty;
}
