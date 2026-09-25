using Hangfire.Server;
using Zyra.LantimeServiceApp.Interfaces;

namespace Zyra.LantimeServiceApp.JobService
{
    public class JobSyncAttendanceJob : ISyncAttendanceJob
    {
        private readonly IAttendanceSyncService _attendanceSyncService;

        public JobSyncAttendanceJob(
            IAttendanceSyncService attendanceSyncService)
        {
            _attendanceSyncService =
                attendanceSyncService ??
                throw new ArgumentNullException(nameof(attendanceSyncService));
        }

        public async Task Execute(PerformContext context)
        {
            await _attendanceSyncService.SyncCheckInAsync(context);
            await _attendanceSyncService.SyncCheckOutAsync(context);
        }
    }
}
