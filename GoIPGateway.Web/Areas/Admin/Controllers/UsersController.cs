using GoIPGateway.Application.DTOs.Identity;
using GoIPGateway.Application.Features.Roles.Queries;
using GoIPGateway.Application.Features.Users.Commands;
using GoIPGateway.Application.Features.Users.Queries;
using GoIPGateway.Web.Models.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class UsersController : Controller
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? tenantId = null, string? role = null, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetUsersListQuery(tenantId, role), cancellationToken);
        var users = result.IsSuccess ? result.Value : new List<UserDto>();

        var rolesResult = await _mediator.Send(new GetRolesListQuery(), cancellationToken);
        var roles = rolesResult.IsSuccess ? rolesResult.Value.Select(r => r.Name).ToList() : new List<string>();

        var list = users.Select(user => new UserListItemViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            TenantId = user.TenantId,
            TenantName = user.TenantName,
            Roles = user.Roles,
            DirectPermissionsCount = user.DirectPermissionsCount,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            CreatedDate = user.CreatedDate,
            LastLoginDate = user.LastLoginDate,
            LastPasswordChangedDate = user.LastPasswordChangedDate
        }).ToList();

        ViewBag.Roles = roles;
        ViewBag.SelectedTenantId = tenantId;
        ViewBag.SelectedRole = role;

        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var metaResult = await _mediator.Send(new GetUserCreateFormMetadataQuery(), cancellationToken);
        if (!metaResult.IsSuccess)
        {
            TempData["Error"] = metaResult.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Tenants = metaResult.Value.Tenants;
        ViewBag.AvailableRoles = metaResult.Value.AvailableRoles;

        var model = new CreateUserViewModel
        {
            TempPassword = metaResult.Value.DefaultTempPassword,
            MustChangePassword = true,
            AutoGeneratePassword = true
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await RepopulateCreateMetadata(cancellationToken);
            return View(model);
        }

        var dto = new CreateUserDto(
            model.FullName,
            model.Email,
            model.PhoneNumber,
            model.TenantId,
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
            await RepopulateCreateMetadata(cancellationToken);
            return View(model);
        }

        TempData["Success"] = $"تم إنشاء المستخدم بنجاح! كلمة المرور المؤقتة هي: {result.Value.GeneratedPassword}";
        TempData["GeneratedPassword"] = result.Value.GeneratedPassword;
        TempData["UserEmail"] = result.Value.Email;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken = default)
    {
        var metaResult = await _mediator.Send(new GetUserEditFormDataQuery(id), cancellationToken);
        if (!metaResult.IsSuccess || metaResult.Value == null)
            return NotFound();

        var user = metaResult.Value;

        ViewBag.Tenants = user.Tenants;
        ViewBag.AvailableRoles = user.AvailableRoles;

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            TenantId = user.TenantId,
            TenantName = user.TenantName,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            SelectedRoles = user.SelectedRoles
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await RepopulateEditMetadata(id, cancellationToken);
            return View(model);
        }

        var dto = new UpdateUserDto(
            model.Id,
            model.FullName,
            model.Email,
            model.PhoneNumber,
            model.TenantId,
            model.SelectedRoles,
            model.IsActive,
            model.MustChangePassword
        );

        var result = await _mediator.Send(new UpdateUserCommand(dto), cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await RepopulateEditMetadata(id, cancellationToken);
            return View(model);
        }

        TempData["Success"] = $"تم تحديث بيانات المستخدم '{model.FullName}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTempPassword(string userId, string? customPassword = null, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new SetUserTempPasswordCommand(userId, customPassword), cancellationToken);
        if (!result.IsSuccess)
        {
            return Json(new { success = false, message = result.Error.Message });
        }

        return Json(new
        {
            success = true,
            message = "تم تعيين كلمة المرور المؤقتة بنجاح وسيُطالب المستخدم بتغييرها فور الدخول.",
            password = result.Value.TemporaryPassword,
            email = result.Value.Email,
            userName = result.Value.FullName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ToggleUserStatusCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = result.Value
            ? "تم تفعيل حساب المستخدم بنجاح."
            : "تم تعطيل حساب المستخدم.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Permissions(string id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetUserPermissionsFormDataQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value == null)
            return NotFound();

        var permData = result.Value;

        var model = new UserPermissionsViewModel
        {
            UserId = permData.UserId,
            FullName = permData.FullName,
            Email = permData.Email,
            TenantId = permData.TenantId,
            TenantName = permData.TenantName,
            UserRoles = permData.UserRoles,
            DirectPermissions = permData.DirectPermissions,
            InheritedRolePermissions = permData.InheritedRolePermissions,
            PermissionGroups = permData.PermissionGroups.Select(g => new PermissionGroupViewModel
            {
                ModuleName = g.ModuleName,
                ModuleKey = g.ModuleKey,
                Permissions = g.Permissions.Select(p => new PermissionItemViewModel
                {
                    Code = p.Code,
                    NameAr = p.NameAr,
                    DescriptionAr = p.DescriptionAr,
                    IsGrantedDirectly = p.IsGranted,
                    IsInheritedFromRole = p.IsInherited,
                    IsTenantAllowed = p.IsTenantAllowed
                }).ToList()
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Permissions(string userId, List<string>? selectedPermissions, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new UpdateUserPermissionsCommand(userId, selectedPermissions ?? new List<string>()), cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error.Message;
            return RedirectToAction(nameof(Permissions), new { id = userId });
        }

        TempData["Success"] = "تم حفظ الصلاحيات المباشرة للمستخدم بنجاح.";
        return RedirectToAction(nameof(Permissions), new { id = userId });
    }

    private async Task RepopulateCreateMetadata(CancellationToken cancellationToken)
    {
        var metaResult = await _mediator.Send(new GetUserCreateFormMetadataQuery(), cancellationToken);
        if (metaResult.IsSuccess)
        {
            ViewBag.Tenants = metaResult.Value.Tenants;
            ViewBag.AvailableRoles = metaResult.Value.AvailableRoles;
        }
    }

    private async Task RepopulateEditMetadata(string id, CancellationToken cancellationToken)
    {
        var metaResult = await _mediator.Send(new GetUserEditFormDataQuery(id), cancellationToken);
        if (metaResult.IsSuccess)
        {
            ViewBag.Tenants = metaResult.Value.Tenants;
            ViewBag.AvailableRoles = metaResult.Value.AvailableRoles;
        }
    }
}
