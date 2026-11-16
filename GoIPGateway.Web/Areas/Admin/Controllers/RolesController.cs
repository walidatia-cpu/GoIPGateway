using GoIPGateway.Application.DTOs.Identity;
using GoIPGateway.Application.Features.Roles.Commands;
using GoIPGateway.Application.Features.Roles.Queries;
using GoIPGateway.Web.Models.Roles;
using GoIPGateway.Web.Models.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class RolesController : Controller
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetRolesListQuery(tenantId, true), cancellationToken);
        var roles = result.IsSuccess ? result.Value : new List<RoleDto>();

        ViewBag.FilteredTenantId = tenantId;

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
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var metaResult = await _mediator.Send(new GetRoleCreateFormMetadataQuery(), cancellationToken);
        if (!metaResult.IsSuccess)
        {
            TempData["Error"] = metaResult.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Tenants = metaResult.Value.Tenants;

        var model = new CreateOrEditRoleViewModel
        {
            PermissionGroups = MapPermissionGroups(metaResult.Value.PermissionGroups)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrEditRoleViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await RepopulateCreateMetadata(model, cancellationToken);
            return View(model);
        }

        var dto = new CreateRoleDto(model.Name, model.Description, model.TenantId, model.SelectedPermissions);
        var result = await _mediator.Send(new CreateRoleCommand(dto), cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await RepopulateCreateMetadata(model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = $"تم إنشاء مجموعة الصلاحيات '{model.Name}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetRoleEditFormDataQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value == null)
            return NotFound();

        var roleData = result.Value;
        ViewBag.Tenants = roleData.Tenants;

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
    public async Task<IActionResult> Edit(string id, CreateOrEditRoleViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await RepopulateEditMetadata(id, model, cancellationToken);
            return View(model);
        }

        var dto = new UpdateRoleDto(model.Id, model.Name, model.Description, model.TenantId, model.SelectedPermissions);
        var result = await _mediator.Send(new UpdateRoleCommand(dto), cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await RepopulateEditMetadata(id, model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = $"تم تحديث مجموعة الصلاحيات '{model.Name}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteRoleCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "تم حذف مجموعة الصلاحيات بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    private async Task RepopulateCreateMetadata(CreateOrEditRoleViewModel model, CancellationToken cancellationToken)
    {
        var metaResult = await _mediator.Send(new GetRoleCreateFormMetadataQuery(), cancellationToken);
        if (metaResult.IsSuccess)
        {
            ViewBag.Tenants = metaResult.Value.Tenants;
            model.PermissionGroups = MapPermissionGroups(metaResult.Value.PermissionGroups, model.SelectedPermissions);
        }
    }

    private async Task RepopulateEditMetadata(string id, CreateOrEditRoleViewModel model, CancellationToken cancellationToken)
    {
        var metaResult = await _mediator.Send(new GetRoleEditFormDataQuery(id), cancellationToken);
        if (metaResult.IsSuccess)
        {
            ViewBag.Tenants = metaResult.Value.Tenants;
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
