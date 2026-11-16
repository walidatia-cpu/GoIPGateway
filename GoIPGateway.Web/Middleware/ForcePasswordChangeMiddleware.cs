using System.Security.Claims;

namespace GoIPGateway.Web.Middleware;

public class ForcePasswordChangeMiddleware
{
    private readonly RequestDelegate _next;

    public ForcePasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var mustChangeClaim = context.User.FindFirst("MustChangePassword")?.Value;
            var mustChange = string.Equals(mustChangeClaim, "True", StringComparison.OrdinalIgnoreCase);

            if (mustChange)
            {
                var path = context.Request.Path.Value ?? string.Empty;

                // Allowed paths while password change is required
                var isAllowed = path.StartsWith("/Account/ChangePasswordRequired", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/Account/SetLanguage", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/fonts/", StringComparison.OrdinalIgnoreCase) ||
                                path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
                                path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                                path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                                path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase);

                if (!isAllowed)
                {
                    if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync("{\"error\":\"يجب تغيير كلمة المرور المؤقتة أولاً قبل استخدام المنصة.\"}");
                        return;
                    }

                    context.Response.Redirect("/Account/ChangePasswordRequired");
                    return;
                }
            }
        }

        await _next(context);
    }
}

