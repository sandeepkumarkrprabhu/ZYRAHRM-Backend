using Hangfire.Dashboard;

namespace ZYRAHRM.IntegrationApp
{
    public static class HangfireUserContext
    {
        public static bool IsAdmin(DashboardContext context)
        {
            var user = context.GetHttpContext().User;
            return user.IsInRole("Pumex\\ZYRAHRMAdmins");
        }
    }
}
