using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;

namespace ZyraHangfireService
{
    public sealed class AttendanceSyncEmailJob : ISyncAttendanceJob
    {
        private readonly JobSyncAttendanceInvoker _invoker;
        private readonly IAttendanceJobEmailNotificationService _notifications;
        private readonly ILogger<AttendanceSyncEmailJob> _logger;

        public AttendanceSyncEmailJob(
            JobSyncAttendanceInvoker invoker,
            IAttendanceJobEmailNotificationService notifications,
            ILogger<AttendanceSyncEmailJob> logger)
        {
            _invoker = invoker;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task Execute(PerformContext context)
        {
            var startedAt = DateTime.Now;
            var status = "Success";
            var details = "Attendance check-in and check-out sync completed.";

            try
            {
                await _invoker.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                status = "Failed";
                details = ex.Message;
                _logger.LogError(ex, "Attendance sync job failed.");
                throw;
            }
            finally
            {
                try
                {
                await _notifications.NotifyAsync(
                    "AttendanceSync",
                    "Attendance Sync",
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

    public sealed class JobSyncAttendanceInvoker
    {
        private readonly IAttendanceSyncService _service;
        public JobSyncAttendanceInvoker(IAttendanceSyncService service) => _service = service;

        public async Task ExecuteAsync(PerformContext context)
        {
            await _service.SyncCheckInAsync(context);
            await _service.SyncCheckOutAsync(context);
        }
    }
}
