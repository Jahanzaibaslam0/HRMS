using HRMS.Services;

namespace HRMS.Middleware;

public class LoginRequiredMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly string[] PublicPrefixes =
    {
        "/login", "/css/", "/js/", "/lib/", "/favicon"
    };

    public LoginRequiredMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AuthService auth, PermissionService perms)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";

        if (IsPublic(path))
        {
            await _next(context);
            return;
        }

        if (!auth.IsLoggedIn)
        {
            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        if (!perms.CanAccessPage(path))
        {
            context.Response.Redirect("/EmployeeMaster?accessDenied=1");
            return;
        }

        if (path.Equals("/auditreport", StringComparison.OrdinalIgnoreCase) && !auth.IsAdmin)
        {
            context.Response.Redirect("/EmployeeMaster?accessDenied=1");
            return;
        }

        await _next(context);
    }

    private static bool IsPublic(string path)
        => PublicPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
}
