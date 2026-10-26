using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            if (User.IsInRole("TenantAdmin") || User.IsInRole("TenantOperator") || User.IsInRole("TenantViewer"))
                return RedirectToAction("Index", "Dashboard", new { area = "Company" });
        }

        return RedirectToAction("Login", "Account");
    }
}
