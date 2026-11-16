using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Security;
using GoIPGateway.Application.DTOs.Identity;
using GoIPGateway.Application.Features.Users.Commands;
using GoIPGateway.Application.Features.Users.Queries;
using GoIPGateway.Infrastructure.Identity.Permissions;
using GoIPGateway.Web.Models.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize]
public class UsersController : Controller
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    public UsersController(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Users.View)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var result = await _mediator.Send(new GetUsersListQuery(tenantId.Value), cancellationToken);
        var users = result.IsSuccess ? result.Value : new List<UserDto>();

        var list = users.Select(u => new UserListItemViewModel
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            TenantId = u.TenantId,
            TenantName = u.TenantName,
            Roles = u.Roles,
            DirectPermissionsCount = u.DirectPermissionsCount,
            IsActive = u.IsActive,
            MustChangePassword = u.MustChangePassword,
            CreatedDate = u.CreatedDate,
            LastLoginDate = u.LastLoginDate,
            LastPasswordChangedDate = u.LastPasswordChangedDate
        }).ToList();

        return View(list);
    }

    [HttpGet]
    [HasPermission(AppPermissions.Users.Create)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var metaResult = await _mediator.Send(new GetUserCreateFormMetadataQuery(tenantId.Value), cancellationToken);
        if (!metaResult.IsSuccess)
        {
            TempData["Error"] = metaResult.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        ViewBag.AvailableRoles = metaResult.Value.AvailableRoles;

        var model = new CreateUserViewModel
        {
            TenantId = tenantId.Value,
            TempPassword = metaResult.Value.DefaultTempPassword,
            MustChangePassword = true,
            AutoGeneratePassword = true
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(AppPermissions.Users.Create)]
    public async Task<IActionResult> Create(CreateUserViewModel model, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        model.TenantId = tenantId.Value;

        if (!ModelState.IsValid)
        {
            await RepopulateCreateMetadata(tenantId.Value, cancellationToken);
            return View(model);
        }

        var dto = new CreateUserDto(
            model.FullName,
            model.Email,
            model.PhoneNumber,
            tenantId.Value,
            model.SelectedRoles,
            model.TempPassword,
            model.AutoGeneratePassword,
            model.MustChangePassword,
            model.IsActive
        );

        var result = await _mediator.Send(new CreateUserCommand(dto), cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await RepopulateCreateMetadata(tenantId.Value, cancellationToken);
            return View(model);
        }

        TempData["Success"] = $"تمت إضافة الموظف '{result.Value.FullName}' بنجاح! كلمة المرور المؤقتة هي: {result.Value.GeneratedPassword}";
        TempData["GeneratedPassword"] = result.Value.GeneratedPassword;
        TempData["UserEmail"] = result.Value.Email;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission(AppPermissions.Users.Edit)]
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var metaResult = await _mediator.Send(new GetUserEditFormDataQuery(id, tenantId.Value), cancellationToken);
        if (!metaResult.IsSuccess || metaResult.Value == null)
            return NotFound();

        var user = metaResult.Value;
        ViewBag.AvailableRoles = user.AvailableRoles;

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            TenantId = user.TenantId,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            SelectedRoles = user.SelectedRoles
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(AppPermissions.Users.Edit)]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return BadRequest();

        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        if (!ModelState.IsValid)
        {
            await RepopulateEditMetadata(id, tenantId.Value, cancellationToken);
            return View(model);
        }

        var dto = new UpdateUserDto(
            model.Id,
            model.FullName,
            model.Email,
            model.PhoneNumber,
            tenantId.Value,
            model.SelectedRoles,
            model.IsActive,
            model.MustChangePassword
        );

        var result = await _mediator.Send(new UpdateUserCommand(dto, tenantId.Value), cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await RepopulateEditMetadata(id, tenantId.Value, cancellationToken);
            return View(model);
        }

        TempData["Success"] = $"تم تحديث بيانات الموظف '{model.FullName}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(AppPermissions.Users.ResetPassword)]
    public async Task<IActionResult> SetTempPassword(string userId, string? customPassword = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var result = await _mediator.Send(new SetUserTempPasswordCommand(userId, customPassword, tenantId.Value), cancellationToken);
        if (!result.IsSuccess)
        {
            return Json(new { success = false, message = result.Error.Message });
        }

        return Json(new
        {
            success = true,
            message = "تم تعيين كلمة المرور المؤقتة للموظف بنجاح.",
            password = result.Value.TemporaryPassword,
            email = result.Value.Email,
            userName = result.Value.FullName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(AppPermissions.Users.Delete)]
    public async Task<IActionResult> ToggleStatus(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var result = await _mediator.Send(new ToggleUserStatusCommand(id, tenantId.Value), cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = result.Value
            ? "تم تفعيل حساب الموظف بنجاح."
            : "تم تعطيل حساب الموظف.";

        return RedirectToAction(nameof(Index));
    }

    private async Task RepopulateCreateMetadata(int tenantId, CancellationToken cancellationToken)
    {
        var metaResult = await _mediator.Send(new GetUserCreateFormMetadataQuery(tenantId), cancellationToken);
        if (metaResult.IsSuccess)
        {
            ViewBag.AvailableRoles = metaResult.Value.AvailableRoles;
        }
    }

    private async Task RepopulateEditMetadata(string id, int tenantId, CancellationToken cancellationToken)
    {
        var metaResult = await _mediator.Send(new GetUserEditFormDataQuery(id, tenantId), cancellationToken);
        if (metaResult.IsSuccess)
        {
            ViewBag.AvailableRoles = metaResult.Value.AvailableRoles;
        }
    }
}
