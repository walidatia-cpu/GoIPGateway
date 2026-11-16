using GoIPGateway.Application.Features.Tenants.Queries;
using GoIPGateway.Application.Features.Users.Commands;
using GoIPGateway.Infrastructure.Identity;
using GoIPGateway.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;

    public AccountController(
        SignInManager<ApplicationUser> signInManager, 
        UserManager<ApplicationUser> userManager,
        IMediator mediator)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _mediator = mediator;
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
                var tenantAccess = await _mediator.Send(new ValidateTenantAccessQuery(user.TenantId.Value));
                if (!tenantAccess.IsSuccess)
                {
                    await _signInManager.SignOutAsync();
                    return View(new LoginViewModel { ReturnUrl = returnUrl });
                }
            }

            if (string.IsNullOrEmpty(returnUrl))
            {
                if (User.IsInRole("SuperAdmin") || User.HasClaim("IsOwner", "True") || User.IsInRole("Owner"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

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

        // Check company status via MediatR and Result Pattern
        if (user.TenantId.HasValue)
        {
            var tenantAccess = await _mediator.Send(new ValidateTenantAccessQuery(user.TenantId.Value));
            if (!tenantAccess.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, tenantAccess.Error.Message);
                return View(model);
            }
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            if (user.MustChangePassword)
            {
                return RedirectToAction(nameof(ChangePasswordRequired));
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) && !model.ReturnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
                return Redirect(model.ReturnUrl);

            var roles = await _userManager.GetRolesAsync(user);
            if (user.IsOwner || roles.Contains("SuperAdmin"))
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
    public async Task<IActionResult> ChangePasswordRequired()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction(nameof(Login));

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction(nameof(Login));

        var model = new ChangePasswordRequiredViewModel
        {
            FullName = user.FullName,
            Email = user.Email
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePasswordRequired(ChangePasswordRequiredViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction(nameof(Login));

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction(nameof(Login));

        model.FullName = user.FullName;
        model.Email = user.Email;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _mediator.Send(new ChangeRequiredPasswordCommand(user.Id, model.CurrentPassword, model.NewPassword));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        // Refresh sign-in to update claims in cookie (removing MustChangePassword)
        await _signInManager.RefreshSignInAsync(user);

        TempData["Success"] = "تم تحديث كلمة المرور بنجاح! تم فتح صلاحيات حسابك بالكامل.";

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("SuperAdmin"))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        return RedirectToAction("Index", "Dashboard", new { area = "Company" });
    }

    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
}
