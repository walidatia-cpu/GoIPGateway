using System.Text.Json;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Features.ServiceDefinitions.Queries;
using GoIPGateway.Application.Features.SimCards.Queries;
using GoIPGateway.Application.Features.Tenants.Queries;
using GoIPGateway.Application.Features.Transactions.Commands;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator")]
public class ExecuteController : Controller
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public ExecuteController(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    private int? ResolveTenantId()
    {
        // Execution of operations is strictly restricted to Company/Tenant accounts only.
        if (_currentUser.IsSuperAdmin || User.IsInRole("SuperAdmin"))
        {
            return null;
        }

        var tenantId = _currentUser.TenantId ?? _tenantProvider.GetCurrentTenantId();
        if (tenantId.HasValue)
        {
            _tenantProvider.SetCurrentTenantId(tenantId.Value);
            return tenantId.Value;
        }

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? serviceCode, int? serviceId, CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId();
        if (!tenantId.HasValue) return Forbid();

        var servicesResult = await _mediator.Send(new GetSubscribedServicesQuery(tenantId.Value), cancellationToken);
        var services = servicesResult.Succeeded ? servicesResult.Data ?? new List<ServiceDefinition>() : new List<ServiceDefinition>();

        var simsResult = await _mediator.Send(new GetCompanySimCardsQuery(tenantId.Value), cancellationToken);
        var sims = simsResult.Succeeded ? simsResult.Data ?? new List<SimCard>() : new List<SimCard>();

        // Determine pre-selected service
        string selectedCode = "";
        if (!string.IsNullOrWhiteSpace(serviceCode))
        {
            var match = services.FirstOrDefault(s => string.Equals(s.ServiceCode, serviceCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null) selectedCode = match.ServiceCode;
        }
        else if (serviceId.HasValue)
        {
            var match = services.FirstOrDefault(s => s.Id == serviceId.Value);
            if (match != null) selectedCode = match.ServiceCode;
        }

        // If no explicit service specified, default to first available service so execution view is immediately loaded
        if (string.IsNullOrEmpty(selectedCode) && services.Any())
        {
            selectedCode = services.First().ServiceCode;
        }

        var vm = new ExecuteServiceViewModel
        {
            Services = services.ToList(),
            AvailableSims = sims.ToList(),
            ServiceCode = selectedCode
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(string serviceCode, int? simCardId, OperatorType? op, string? externalRef, [FromForm] IFormCollection form, CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId();
        if (!tenantId.HasValue) return Forbid();

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in form.Keys)
        {
            if (key.StartsWith("param_"))
            {
                var realKey = key["param_".Length..];
                var val = form[key].ToString()?.Trim();
                if (!string.IsNullOrEmpty(val))
                {
                    parameters[realKey] = val;
                }
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

        var result = await _mediator.Send(command, cancellationToken);

        // Check if AJAX request
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
        {
            if (result.IsSuccess)
                return Json(new { success = true, data = result.Value });
            else
                return Json(new { success = false, errorCode = result.Error.Code, message = result.Error.Message });
        }

        if (result.IsSuccess)
        {
            TempData["Success"] = $"تم تنفيذ العملية بنجاح! رقم المرجع: {result.Value.TransactionRef}.";
            TempData["ExecutionResultJson"] = JsonSerializer.Serialize(result.Value);
        }
        else
        {
            TempData["Error"] = $"فشل تنفيذ العملية: {result.Error.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// نقطة نهاية للاختبار الفوري السريع عبر AJAX بدون إعادة تحميل الصفحة
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> QuickTest([FromBody] ExecuteServiceRequestDto request, CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId();
        if (!tenantId.HasValue) return Forbid();

        var parameters = request.GetNormalizedParameters();
        var command = new ExecuteServiceCommand(
            request.ServiceCode,
            request.ExternalRef,
            request.SimCardId,
            request.Operator,
            parameters,
            request.CallbackUrl,
            RequestSource.Dashboard
        );

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
            return Ok(new { success = true, data = result.Value });

        return BadRequest(new { success = false, errorCode = result.Error.Code, message = result.Error.Message });
    }
}