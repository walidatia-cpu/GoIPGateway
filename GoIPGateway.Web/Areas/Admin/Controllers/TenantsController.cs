using GoIPGateway.Application.Features.ServiceDefinitions.Queries;
using GoIPGateway.Application.Features.Tenants.Commands;
using GoIPGateway.Application.Features.Tenants.Queries;
using GoIPGateway.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : Controller
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTenantsListQuery(), cancellationToken);
        var tenants = result.Succeeded ? result.Data ?? new List<Tenant>() : new List<Tenant>();
        return View(tenants);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateTenantCommand("", "", "", "", "", ""));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            TempData["Success"] = "تمت إضافة الشركة بنجاح وإنشاء مفتاح API افتراضي.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error?.Message ?? "فشلت عملية إنشاء الشركة.");
        return View(command);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(id), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Tenant model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();

        var command = new UpdateTenantCommand(
            model.Id,
            model.CompanyName,
            model.CompanyNameEn,
            model.ContactPerson,
            model.ContactEmail,
            model.ContactPhone,
            model.Address,
            model.TaxId,
            model.MaxDevices,
            model.MaxDailyOperations,
            model.MaxMonthlyOps,
            model.CreditLimit,
            model.IsActive,
            model.IsBlocked,
            model.BlockReason);

        var result = await _mediator.Send(command, cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = $"تم تحديث بيانات شركة '{model.CompanyName}' بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error?.Message ?? "فشلت عملية التحديث.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(id), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ManageServices(int id, CancellationToken cancellationToken)
    {
        var tenantResult = await _mediator.Send(new GetTenantByIdQuery(id), cancellationToken);
        if (!tenantResult.Succeeded || tenantResult.Data == null)
            return NotFound();

        var servicesResult = await _mediator.Send(new GetServiceDefinitionsQuery(), cancellationToken);
        var services = servicesResult.Succeeded ? servicesResult.Data ?? new List<ServiceDefinition>() : new List<ServiceDefinition>();

        var tenant = tenantResult.Data;
        ViewBag.Tenant = tenant;
        ViewBag.SubscribedIds = tenant.SubscribedServices.Where(s => s.IsActive).Select(s => s.ServiceDefinitionId).ToList();

        return View(services);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveServices(int tenantId, int[]? selectedServiceIds, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SaveTenantServicesCommand(tenantId, selectedServiceIds), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم حفظ وتحديث الخدمات المتاحة للشركة بنجاح.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل حفظ الخدمات المتاحة.";
        }

        return RedirectToAction(nameof(ManageServices), new { id = tenantId });
    }

    [HttpGet]
    public async Task<IActionResult> ManageTokens(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(id), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        ViewBag.Tenant = result.Data;
        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> CreateToken(int tenantId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(tenantId), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        ViewBag.Tenant = result.Data;
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateToken(int tenantId, string keyName, int[]? allowedServiceIds, int rateLimit = 100, int expiryDays = 365, int? expiryMonths = null, CancellationToken cancellationToken = default)
    {
        var effectiveDays = expiryDays > 0 ? expiryDays : (expiryMonths.HasValue && expiryMonths.Value > 0 ? expiryMonths.Value * 30 : 365);
        var command = new CreateTenantTokenCommand(tenantId, keyName, allowedServiceIds, rateLimit, effectiveDays);

        var result = await _mediator.Send(command, cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = $"تم توليد التوكن '{keyName}' بنجاح وتعيين صلاحيات الخدمات له.";
            return RedirectToAction(nameof(ManageTokens), new { id = tenantId });
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل إنشاء التوكن.";
            return RedirectToAction(nameof(CreateToken), new { tenantId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> TokenDetails(int id, int tenantId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(tenantId), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        var tenant = result.Data;
        var apiKey = tenant.ApiKeys.FirstOrDefault(k => k.Id == id);
        if (apiKey == null)
            return NotFound();

        ViewBag.Tenant = tenant;
        return View(apiKey);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeToken(int id, int tenantId, string? returnAction = null, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new RevokeApiKeyCommand(id, tenantId), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم تعطيل التوكن بنجاح.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل تعطيل التوكن.";
        }

        if (string.Equals(returnAction, "TokenDetails", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(TokenDetails), new { id, tenantId });

        return RedirectToAction(nameof(ManageTokens), new { id = tenantId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateToken(int id, int tenantId, string? returnAction = null, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ActivateApiKeyCommand(id, tenantId), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم تفعيل التوكن بنجاح.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل تفعيل التوكن.";
        }

        if (string.Equals(returnAction, "TokenDetails", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(TokenDetails), new { id, tenantId });

        return RedirectToAction(nameof(ManageTokens), new { id = tenantId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteToken(int id, int tenantId, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteApiKeyCommand(id, tenantId), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم حذف التوكن نهائياً.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل حذف التوكن.";
        }

        return RedirectToAction(nameof(ManageTokens), new { id = tenantId });
    }

    [HttpPost]
    public async Task<IActionResult> GenerateApiKey(int tenantId, string keyName, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GenerateApiKeyCommand(tenantId, keyName), cancellationToken);
        if (result.IsSuccess)
        {
            TempData["Success"] = $"تم إنشاء المفتاح بنجاح: {result.Value.ApiKey}";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل توليد المفتاح.";
        }

        return RedirectToAction(nameof(Index));
    }
}