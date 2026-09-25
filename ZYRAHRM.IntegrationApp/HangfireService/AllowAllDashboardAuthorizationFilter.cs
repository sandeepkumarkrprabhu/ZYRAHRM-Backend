using Hangfire.Dashboard;

namespace ZYRAHRM.IntegrationApp.HangfireService
{
    public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}
}
