using System.ComponentModel.DataAnnotations;

namespace GoIPGateway.Web.Models.Users;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public List<string> Roles { get; set; } = new();
    public int DirectPermissionsCount { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public DateTime? LastPasswordChangedDate { get; set; }
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "الاسم الكامل مطلوب")]
    [Display(Name = "الاسم الكامل")]
    [StringLength(150, ErrorMessage = "الاسم يجب ألا يتجاوز {1} حرفاً.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
    [Display(Name = "رقم الهاتف")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "الشركة التابع لها")]
    public int? TenantId { get; set; }

    [Display(Name = "الأدوار والمجموعات")]
    public List<string> SelectedRoles { get; set; } = new();

    [Display(Name = "كلمة المرور المؤقتة")]
    public string? TempPassword { get; set; }

    [Display(Name = "توليد كلمة مرور مؤقتة تلقائياً")]
    public bool AutoGeneratePassword { get; set; } = true;

    [Display(Name = "إلزام المستخدم بتغيير كلمة المرور عند أول تسجيل دخول")]
    public bool MustChangePassword { get; set; } = true;

    [Display(Name = "الحساب نشط")]
    public bool IsActive { get; set; } = true;
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "الاسم الكامل مطلوب")]
    [Display(Name = "الاسم الكامل")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
    [Display(Name = "رقم الهاتف")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "الشركة التابع لها")]
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }

    [Display(Name = "الأدوار والمجموعات")]
    public List<string> SelectedRoles { get; set; } = new();

    [Display(Name = "الحساب نشط ومفعل")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "إلزام بتغيير كلمة المرور")]
    public bool MustChangePassword { get; set; }
}

public class UserPermissionsViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public List<string> UserRoles { get; set; } = new();
    
    // Direct permission codes assigned directly to this user
    public List<string> DirectPermissions { get; set; } = new();

    // Permission codes inherited from assigned roles
    public List<string> InheritedRolePermissions { get; set; } = new();

    // Grouped permission catalog
    public List<PermissionGroupViewModel> PermissionGroups { get; set; } = new();
}

public class PermissionGroupViewModel
{
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public List<PermissionItemViewModel> Permissions { get; set; } = new();
}

public class PermissionItemViewModel
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public bool IsGrantedDirectly { get; set; }
    public bool IsInheritedFromRole { get; set; }
    public bool IsTenantAllowed { get; set; }
}

