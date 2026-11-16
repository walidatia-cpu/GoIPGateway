using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Security;
using GoIPGateway.Application.DTOs.Identity;
using GoIPGateway.Application.Features.Roles.Commands;
using GoIPGateway.Application.Features.Roles.Queries;
using GoIPGateway.Infrastructure.Identity.Permissions;
using GoIPGateway.Web.Models.Roles;
using GoIPGateway.Web.Models.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize]
public class RolesController : Controller
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    public RolesController(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Roles.View)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var result = await _mediator.Send(new GetRolesListQuery(tenantId.Value, false), cancellationToken);
        var roles = result.IsSuccess ? result.Value : new List<RoleDto>();

        var list = roles.Select(r => new RoleListItemViewModel
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            TenantId = r.TenantId,
            TenantName = r.TenantName,
            IsSystemRole = r.IsSystemRole,
            UsersCount = r.UsersCount,
            PermissionsCount = r.PermissionsCount
        }).ToList();

        return View(list);
    }

    [HttpGet]
    [HasPermission(AppPermissions.Roles.Create)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var metaResult = await _mediator.Send(new GetRoleCreateFormMetadataQuery(tenantId.Value, false), cancellationToken);
        if (!metaResult.IsSuccess)
        {
            TempData["Error"] = metaResult.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        var model = new CreateOrEditRoleViewModel
        {
            TenantId = tenantId.Value,
            PermissionGroups = MapPermissionGroups(metaResult.Value.PermissionGroups)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(AppPermissions.Roles.Create)]
    public async Task<IActionResult> Create(CreateOrEditRoleViewModel model, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        model.TenantId = tenantId.Value;

        if (!ModelState.IsValid)
        {
            await RepopulateCreateMetadata(tenantId.Value, model, cancellationToken);
            return View(model);
        }

        var dto = new CreateRoleDto(model.Name.Trim(), model.Description, tenantId.Value, model.SelectedPermissions);
        var result = await _mediator.Send(new CreateRoleCommand(dto), cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await RepopulateCreateMetadata(tenantId.Value, model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = $"تم إنشاء مجموعة الصلاحيات '{model.Name}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission(AppPermissions.Roles.Edit)]
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var result = await _mediator.Send(new GetRoleEditFormDataQuery(id, tenantId.Value, false), cancellationToken);
        if (!result.IsSuccess || result.Value == null)
            return NotFound();

        var roleData = result.Value;

        var model = new CreateOrEditRoleViewModel
        {
            Id = roleData.Id,
            Name = roleData.Name,
            Description = roleData.Description,
            TenantId = roleData.TenantId,
            TenantName = roleData.TenantName,
            IsSystemRole = roleData.IsSystemRole,
            SelectedPermissions = roleData.SelectedPermissions,
            PermissionGroups = MapPermissionGroups(roleData.PermissionGroups, roleData.SelectedPermissions)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(AppPermissions.Roles.Edit)]
    public async Task<IActionResult> Edit(string id, CreateOrEditRoleViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return BadRequest();

        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        if (!ModelState.IsValid)
        {
            await RepopulateEditMetadata(id, tenantId.Value, model, cancellationToken);
            return View(model);
        }

        var dto = new UpdateRoleDto(model.Id, model.Name.Trim(), model.Description, tenantId.Value, model.SelectedPermissions);
        var result = await _mediator.Send(new UpdateRoleCommand(dto, tenantId.Value), cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await RepopulateEditMetadata(id, tenantId.Value, model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = $"تم تحديث مجموعة الصلاحيات '{model.Name}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(AppPermissions.Roles.Delete)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var result = await _mediator.Send(new DeleteRoleCommand(id, tenantId.Value), cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "تم حذف مجموعة الصلاحيات بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    private async Task RepopulateCreateMetadata(int tenantId, CreateOrEditRoleViewModel model, CancellationToken cancellationToken)
    {
        var metaResult = await _mediator.Send(new GetRoleCreateFormMetadataQuery(tenantId, false), cancellationToken);
        if (metaResult.IsSuccess)
        {
            model.PermissionGroups = MapPermissionGroups(metaResult.Value.PermissionGroups, model.SelectedPermissions);
        }
    }

    private async Task RepopulateEditMetadata(string id, int tenantId, CreateOrEditRoleViewModel model, CancellationToken cancellationToken)
    {
        var metaResult = await _mediator.Send(new GetRoleEditFormDataQuery(id, tenantId, false), cancellationToken);
        if (metaResult.IsSuccess)
        {
            model.PermissionGroups = MapPermissionGroups(metaResult.Value.PermissionGroups, model.SelectedPermissions);
        }
    }

    private static List<PermissionGroupViewModel> MapPermissionGroups(List<PermissionGroupDto> dtos, List<string>? selectedPermissions = null)
    {
        var selectedSet = (selectedPermissions ?? new List<string>()).ToHashSet();
        return dtos.Select(g => new PermissionGroupViewModel
        {
            ModuleName = g.ModuleName,
            ModuleKey = g.ModuleKey,
            Permissions = g.Permissions.Select(p => new PermissionItemViewModel
            {
                Code = p.Code,
                NameAr = p.NameAr,
                DescriptionAr = p.DescriptionAr,
                IsGrantedDirectly = selectedSet.Contains(p.Code) || p.IsGranted,
                IsInheritedFromRole = p.IsInherited,
                IsTenantAllowed = p.IsTenantAllowed
            }).ToList()
        }).ToList();
    }
}
