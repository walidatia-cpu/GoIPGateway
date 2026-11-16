using System.ComponentModel.DataAnnotations;
using GoIPGateway.Web.Models.Users;

namespace GoIPGateway.Web.Models.Roles;

public class RoleListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public bool IsSystemRole { get; set; }
    public int UsersCount { get; set; }
    public int PermissionsCount { get; set; }
}

public class CreateOrEditRoleViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "اسم المجموعة أو الدور مطلوب")]
    [Display(Name = "اسم الدور أو المجموعة")]
    [StringLength(100, ErrorMessage = "اسم الدور يجب ألا يتجاوز {1} حرفاً.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "الوصف")]
    [StringLength(250, ErrorMessage = "الوصف يجب ألا يتجاوز {1} حرفاً.")]
    public string? Description { get; set; }

    [Display(Name = "الشركة التابع لها")]
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }

    public bool IsSystemRole { get; set; }

    [Display(Name = "الصلاحيات المحددة")]
    public List<string> SelectedPermissions { get; set; } = new();

    public List<PermissionGroupViewModel> PermissionGroups { get; set; } = new();
}

