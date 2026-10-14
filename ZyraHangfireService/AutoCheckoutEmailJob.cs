using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;

namespace ZyraHangfireService
{
    public sealed class AutoCheckoutEmailJob : IAutoCheckoutJob
    {
        private readonly JobAutoCheckoutInvoker _invoker;
        private readonly IAttendanceJobEmailNotificationService _notifications;
        private readonly ILogger<AutoCheckoutEmailJob> _logger;

        public AutoCheckoutEmailJob(
            JobAutoCheckoutInvoker invoker,
            IAttendanceJobEmailNotificationService notifications,
            ILogger<AutoCheckoutEmailJob> logger)
        {
            _invoker = invoker;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task Execute(PerformContext context)
        {
            var startedAt = DateTime.Now;
            var status = "Success";
            var details = "Auto checkout job completed. Review Hangfire logs for employee-level outcomes.";

            try
            {
                await _invoker.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                status = "Failed";
                details = ex.Message;
                _logger.LogError(ex, "Auto checkout job failed.");
                throw;
            }
            finally
            {
                await _notifications.NotifyAsync(
                    "AutoCheckout",
                    "Attendance Auto Checkout",
                    status,
                    startedAt,
                    DateTime.Now,
                    details);
            }
        }
    }

    public sealed class JobAutoCheckoutInvoker
    {
        private readonly Zyra.LantimeServiceApp.JobService.JobAutoCheckoutJob _job;
        public JobAutoCheckoutInvoker(Zyra.LantimeServiceApp.JobService.JobAutoCheckoutJob job) => _job = job;
        public Task ExecuteAsync(PerformContext context) => _job.Execute(context);
    }
}
