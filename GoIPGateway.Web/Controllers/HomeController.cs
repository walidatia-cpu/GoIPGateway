using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("SuperAdmin") || User.HasClaim("IsOwner", "True") || User.IsInRole("Owner"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            return RedirectToAction("Index", "Dashboard", new { area = "Company" });
        }

        return RedirectToAction("Login", "Account");
    }
}
