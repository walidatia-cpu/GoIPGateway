using System.Text.Json;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Features.ServiceDefinitions.Commands;
using GoIPGateway.Application.Features.ServiceDefinitions.Queries;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class ServicesController : Controller
{
    private readonly IMediator _mediator;

    public ServicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceDefinitionsQuery(), cancellationToken);
        var services = result.Succeeded ? result.Data ?? new List<ServiceDefinition>() : new List<ServiceDefinition>();
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
    public async Task<IActionResult> Create(ServiceDefinition model, CancellationToken cancellationToken)
    {
        model.ServiceCode = model.ServiceCode?.Trim().ToUpperInvariant() ?? "";
        model.NameAr = model.NameAr?.Trim() ?? "";
        model.NameEn = string.IsNullOrWhiteSpace(model.NameEn) ? null : model.NameEn.Trim();
        if (model.ServiceType == 0) model.ServiceType = ServiceType.USSD;

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

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "يرجى التحقق من صحة البيانات المدخلة.";
            TempData["Error"] = firstError;
            return View(model);
        }

        var result = await _mediator.Send(new CreateServiceDefinitionCommand(model), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = $"تمت إضافة الخدمة '{model.NameAr}' بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error?.Message ?? "تعذر إضافة الخدمة.");
        TempData["Error"] = result.Error?.Message ?? "تعذر إضافة الخدمة.";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceWithStepsAndParamsQuery(id), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceWithStepsAndParamsQuery(id), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceDefinition model, CancellationToken cancellationToken)
    {
        model.ServiceCode = model.ServiceCode?.Trim().ToUpperInvariant() ?? "";
        model.NameAr = model.NameAr?.Trim() ?? "";
        model.NameEn = string.IsNullOrWhiteSpace(model.NameEn) ? null : model.NameEn.Trim();

        if (string.IsNullOrWhiteSpace(model.ServiceCode))
            ModelState.AddModelError("ServiceCode", "كود الخدمة مطلوب.");

        if (string.IsNullOrWhiteSpace(model.NameAr))
            ModelState.AddModelError("NameAr", "اسم الخدمة بالعربية مطلوب.");

        if (!model.Operator.HasValue)
            ModelState.AddModelError("Operator", "يرجى تحديد شركة المحمول.");

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "بيانات غير صالحة.";
            TempData["Error"] = firstError;
            return View(model);
        }

        var result = await _mediator.Send(new UpdateServiceDefinitionCommand(id, model), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = $"تم حفظ تعديلات الخدمة '{model.NameAr}' بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error?.Message ?? "فشل حفظ التعديلات.");
        TempData["Error"] = result.Error?.Message ?? "فشل حفظ التعديلات.";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ToggleServiceStatusCommand(id), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم تحديث حالة تفعيل الخدمة بنجاح.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل تحديث حالة الخدمة.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteServiceDefinitionCommand(id), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم حذف الخدمة بنجاح.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "فشل حذف الخدمة.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ConfigureSteps(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceWithStepsAndParamsQuery(id), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> CreateStep(int serviceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceWithStepsAndParamsQuery(serviceId), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        var service = result.Data;
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
        string[]? failureClientMessages,
        CancellationToken cancellationToken)
    {
        step.ServiceDefinitionId = serviceId;
        PackStepRules(step, successPatterns, successClientMessages, failurePatterns, failureClientMessages);

        var result = await _mediator.Send(new AddServiceStepCommand(serviceId, step), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تمت إضافة خطوة USSD جديدة بنجاح.";
            return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
        }

        TempData["Error"] = GetResultErrorMessage(result, "فشل إضافة الخطوة.");
        return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
    }

    [HttpGet]
    public async Task<IActionResult> EditStep(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceStepByIdQuery(id), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        var step = result.Data;
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
        string[]? failureClientMessages,
        CancellationToken cancellationToken)
    {
        PackStepRules(model, successPatterns, successClientMessages, failurePatterns, failureClientMessages);

        var result = await _mediator.Send(new UpdateServiceStepCommand(id, model), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم تعديل خطوة USSD بنجاح.";
            return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
        }

        TempData["Error"] = GetResultErrorMessage(result, "فشل تعديل الخطوة.");
        return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
    }

    [HttpGet]
    public async Task<IActionResult> StepDetails(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceStepByIdQuery(id), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        ViewBag.Service = result.Data.ServiceDefinition;
        return View(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStep(int id, int serviceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteServiceStepCommand(id, serviceId), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم حذف خطوة الـ USSD بنجاح.";
        }
        else
        {
            TempData["Error"] = GetResultErrorMessage(result, "فشل حذف الخطوة.");
        }

        return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddParameter(int serviceId, ServiceParameter param, CancellationToken cancellationToken)
    {
        param.ServiceDefinitionId = serviceId;
        param.ParameterKey = (param.ParameterKey ?? string.Empty).Trim().Trim('{', '}').Trim();
        if (string.IsNullOrWhiteSpace(param.ParameterName))
        {
            param.ParameterName = !string.IsNullOrWhiteSpace(param.ParameterKey)
                ? (char.ToUpper(param.ParameterKey[0]) + (param.ParameterKey.Length > 1 ? param.ParameterKey[1..] : ""))
                : (!string.IsNullOrWhiteSpace(param.DisplayNameAr) ? param.DisplayNameAr : "Param");
        }
        if (string.IsNullOrWhiteSpace(param.DisplayNameAr))
        {
            param.DisplayNameAr = param.ParameterName;
        }

        var result = await _mediator.Send(new AddServiceParameterCommand(param), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = $"تمت إضافة البارامتر '{param.DisplayNameAr}' ({{{param.ParameterKey}}}) بنجاح.";
        }
        else
        {
            TempData["Error"] = GetResultErrorMessage(result, "فشل إضافة البارامتر.");
        }

        return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteParameter(int id, int serviceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteServiceParameterCommand(id, serviceId), cancellationToken);
        if (result.Succeeded)
        {
            TempData["Success"] = "تم حذف البارامتر بنجاح.";
        }
        else
        {
            TempData["Error"] = GetResultErrorMessage(result, "فشل حذف البارامتر.");
        }

        return RedirectToAction(nameof(ConfigureSteps), new { id = serviceId });
    }

    private static string GetResultErrorMessage(Result result, string defaultMsg)
    {
        if (result.ValidationErrors != null && result.ValidationErrors.Any())
            return string.Join(" | ", result.ValidationErrors.Select(v => v.Message));

        return !string.IsNullOrWhiteSpace(result.Error?.Message) ? result.Error.Message : defaultMsg;
    }

    private static void PackStepRules(
        ServiceUssdStep target, 
        string[]? successPatterns, 
        string[]? successClientMessages, 
        string[]? failurePatterns, 
        string[]? failureClientMessages)
    {
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
}