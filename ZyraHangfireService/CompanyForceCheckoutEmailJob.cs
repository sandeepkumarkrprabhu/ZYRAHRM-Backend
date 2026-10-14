using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;

namespace ZyraHangfireService
{
    public sealed class CompanyForceCheckoutEmailJob : ICompanyForceCheckoutJob
    {
        private readonly JobCompanyForceCheckoutInvoker _invoker;
        private readonly IAttendanceJobEmailNotificationService _notifications;
        private readonly ILogger<CompanyForceCheckoutEmailJob> _logger;

        public CompanyForceCheckoutEmailJob(
            JobCompanyForceCheckoutInvoker invoker,
            IAttendanceJobEmailNotificationService notifications,
            ILogger<CompanyForceCheckoutEmailJob> logger)
        {
            _invoker = invoker;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task Execute(PerformContext context)
        {
            var startedAt = DateTime.Now;
            var status = "Success";
            var details = "Company force checkout job completed. Review Hangfire logs for employee-level outcomes.";

            try
            {
                await _invoker.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                status = "Failed";
                details = ex.Message;
                _logger.LogError(ex, "Company force checkout job failed.");
                throw;
            }
            finally
            {
                await _notifications.NotifyAsync(
                    "CompanyForceCheckout",
                    "Company Force Checkout",
                    status,
                    startedAt,
                    DateTime.Now,
                    details);
            }
        }
    }

    public sealed class JobCompanyForceCheckoutInvoker
    {
        private readonly ICompanyForceCheckoutJob _job;
        public JobCompanyForceCheckoutInvoker(ICompanyForceCheckoutJob job) => _job = job;
        public Task ExecuteAsync(PerformContext context) => _job.Execute(context);
    }
}
