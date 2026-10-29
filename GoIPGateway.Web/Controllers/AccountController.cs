using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Infrastructure.Identity;
using GoIPGateway.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public AccountController(
        SignInManager<ApplicationUser> signInManager, 
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // Sanitize returnUrl to prevent self-referencing redirect loops
        if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            returnUrl = null;
        }

        // Only auto-redirect if there is no explicit returnUrl requested
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && user.TenantId.HasValue)
            {
                var tenant = await _unitOfWork.Tenants.AsQueryable()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == user.TenantId.Value);

                if (tenant == null || tenant.IsDelete || !tenant.IsActive || tenant.IsBlocked)
                {
                    await _signInManager.SignOutAsync();
                    return View(new LoginViewModel { ReturnUrl = returnUrl });
                }
            }

            if (string.IsNullOrEmpty(returnUrl))
            {
                if (User.IsInRole("SuperAdmin"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

                if (User.IsInRole("TenantAdmin") || User.IsInRole("TenantOperator") || User.IsInRole("TenantViewer"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Company" });
            }
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة أو الحساب غير مفعل.");
            return View(model);
        }

        // Check company status: Block login if tenant is deleted or blocked
        if (user.TenantId.HasValue)
        {
            var tenant = await _unitOfWork.Tenants.AsQueryable()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == user.TenantId.Value);

            if (tenant == null || tenant.IsDelete)
            {
                ModelState.AddModelError(string.Empty, "حساب الشركة التابع لها هذا المستخدم محذوف من النظام، ولا يمكن تسجيل الدخول.");
                return View(model);
            }

            if (!tenant.IsActive || tenant.IsBlocked)
            {
                var reason = !string.IsNullOrWhiteSpace(tenant.BlockReason) ? $" ({tenant.BlockReason})" : "";
                ModelState.AddModelError(string.Empty, $"حساب الشركة موقوف حالياً من قبل الإدارة{reason}. يرجى مراجعة إدارة المنصة.");
                return View(model);
            }
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) && !model.ReturnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
                return Redirect(model.ReturnUrl);

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            return RedirectToAction("Index", "Dashboard", new { area = "Company" });
        }

        ModelState.AddModelError(string.Empty, "فشل تسجيل الدخول. تأكد من البريد وكلمة المرور.");
        return View(model);
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult SetLanguage(string culture, string? returnUrl = null)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
