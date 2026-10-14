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
                try
                {
                await _notifications.NotifyAsync(
                    "CompanyForceCheckout",
                    "Company Force Checkout",
                    status,
                    startedAt,
                    DateTime.Now,
                    details);
                }
                catch (Exception notificationException)
                {
                    _logger.LogError(notificationException, "Failed to send job notification; original job result is preserved.");
                }
            }
        }
    }

    public sealed class JobCompanyForceCheckoutInvoker
    {
        private readonly Zyra.LantimeServiceApp.JobService.JobCompanyForceCheckout _job;
        public JobCompanyForceCheckoutInvoker(Zyra.LantimeServiceApp.JobService.JobCompanyForceCheckout job) => _job = job;
        public Task ExecuteAsync(PerformContext context) => _job.Execute(context);
    }
}
