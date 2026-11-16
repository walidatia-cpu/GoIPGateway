using System.ComponentModel.DataAnnotations;

namespace GoIPGateway.Web.Models;

public class ChangePasswordRequiredViewModel
{
    public string? FullName { get; set; }
    public string? Email { get; set; }

    [Required(ErrorMessage = "كلمة المرور المؤقتة الحالية مطلوبة")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور المؤقتة")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [StringLength(100, ErrorMessage = "يجب ألا تقل كلمة المرور عن {2} أحرف.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور الجديدة")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور الجديدة")]
    [Compare("NewPassword", ErrorMessage = "كلمة المرور وتأكيد كلمة المرور غير متطابقين.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

