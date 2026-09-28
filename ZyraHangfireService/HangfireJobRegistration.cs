using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ZYRA.Attendance.Infrastructure;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace ZyraHangfireService
{
    public static class HangfireJobRegistration
    {
        private const string CompanyForceCheckoutEnabledSetting = "CompanyForceCheckoutEnabled";
        private const string CompanyForceCheckoutJobId = "ProcessCompanyForceCheckoutJob";

        public static void Register(IServiceProvider provider, TimeZoneInfo istZone)
        {
            var settings = provider.GetRequiredService<IOptions<BiometricSyncSettings>>().Value;
            var dbContext = provider.GetRequiredService<AttendanceDbContext>();

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
            // COMPANY FORCE CHECKOUT JOB
            // -------------------------------
            var enabledValue = dbContext.HRMSettings
                .AsNoTracking()
                .Where(x => x.SettingsName == CompanyForceCheckoutEnabledSetting)
                .Select(x => x.SettingsValue)
                .FirstOrDefault();

            var companyForceCheckoutEnabled =
                bool.TryParse(enabledValue, out var parsedEnabled) && parsedEnabled;

            if (companyForceCheckoutEnabled)
            {
                RecurringJob.AddOrUpdate<ICompanyForceCheckoutJob>(
                    CompanyForceCheckoutJobId,
                    x => x.Execute(null),
                    settings.CompanyForceCheckoutJobCron,
                    new RecurringJobOptions { TimeZone = istZone });
            }
            else
            {
                RecurringJob.RemoveIfExists(CompanyForceCheckoutJobId);
            }

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