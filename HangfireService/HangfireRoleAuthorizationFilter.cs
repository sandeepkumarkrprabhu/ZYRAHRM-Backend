using Hangfire.Dashboard;
using Microsoft.Extensions.Options;
using Zyra.LantimeServiceApp.Models;

public class HangfireRoleAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly HangfireSecurityOptions _options;

    public HangfireRoleAuthorizationFilter(IOptions<HangfireSecurityOptions> options)
    {
        _options = options.Value;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // ✅ Allow localhost (dev scenario)
        if (httpContext.Connection.RemoteIpAddress?.ToString() == "127.0.0.1" ||
            httpContext.Connection.RemoteIpAddress?.ToString() == "::1")
        {
            return true;
        }

        var user = httpContext.User;

        if (user?.Identity == null || !user.Identity.IsAuthenticated)
            return false;

        return user.IsInRole(_options.AdminGroup) ||
               user.IsInRole(_options.UserGroup);
    }
}