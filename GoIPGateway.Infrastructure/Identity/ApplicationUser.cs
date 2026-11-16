using Microsoft.AspNetCore.Identity;

namespace GoIPGateway.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public int? TenantId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public bool MustChangePassword { get; set; } = false;
    public DateTime? TemporaryPasswordSetDate { get; set; }
    public DateTime? LastPasswordChangedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }

    /// <summary>
    /// مالك المنصة (System Owner) - يتمتع بأعلى وأشمل صلاحيات في النظام ومحمي من التعديل أو الإيقاف ومخفي من القوائم
    /// </summary>
    public bool IsOwner { get; set; } = false;
}
