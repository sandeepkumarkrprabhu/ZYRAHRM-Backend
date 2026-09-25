using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace ZyraHangfireService
{
    public static class HangfireJobRegistration
    {
        public static void Register(IServiceProvider provider, TimeZoneInfo istZone)
        {
            var settings = provider.GetRequiredService<IOptions<BiometricSyncSettings>>().Value;

            // -------------------------------
            // ATTENDANCE SYNC JOB
            // -------------------------------
            RecurringJob.AddOrUpdate<ISyncAttendanceJob>(
                "ProcessAttendanceSyncJob",
                x => x.Execute(null),
                settings.AttendanceSyncJobCron,
                new RecurringJobOptions { TimeZone = istZone });

            // -------------------------------
            // AUTO CHECKOUT JOB
            // -------------------------------
            RecurringJob.AddOrUpdate<IAutoCheckoutJob>(
                "ProcessAutoCheckoutJob",
                x => x.Execute(null),
                settings.AutoCheckoutJobCron,
                new RecurringJobOptions { TimeZone = istZone });

            // -------------------------------
            // DIRECTOR ATTENDANCE JOB
            // -------------------------------
            RecurringJob.AddOrUpdate<IDirectorAttendanceJob>(
                "ProcessDirectorAttendanceJob",
                x => x.Execute(null),
                settings.DirectorAttendanceJobCron,
                new RecurringJobOptions { TimeZone = istZone });

            // -------------------------------
            // EMPLOYEE MASTER SYNC JOB
            // -------------------------------
            RecurringJob.AddOrUpdate<IEmployeeSyncJob>(
                "ProcessEmployeeMasterSyncJob",
                x => x.Execute(null),
                settings.EmployeeMasterSyncJobCron,
                new RecurringJobOptions { TimeZone = istZone });

            // -------------------------------
            // PUNCH TIME UPDATE JOB
            // -------------------------------
            RecurringJob.AddOrUpdate<IEmployeePunchSyncJob>(
                "ProcessEmployeePunchTimeUpdateJob",
                x => x.Execute(null),
                settings.EmployeePunchTimeUpdateJobCron,
                new RecurringJobOptions { TimeZone = istZone });
        }
    }
}
