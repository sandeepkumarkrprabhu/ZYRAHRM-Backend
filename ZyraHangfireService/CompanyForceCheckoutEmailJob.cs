using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Constants;
using Zyra.LantimeServiceApp.Interfaces;
using ZYRA.Attendance.Infrastructure;

namespace ZyraHangfireService
{
    public sealed class CompanyForceCheckoutEmailJob : ICompanyForceCheckoutJob
    {
        private readonly JobCompanyForceCheckoutInvoker _invoker;
        private readonly IAttendanceJobEmailNotificationService _notifications;
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<CompanyForceCheckoutEmailJob> _logger;

        public CompanyForceCheckoutEmailJob(
            JobCompanyForceCheckoutInvoker invoker,
            IAttendanceJobEmailNotificationService notifications,
            AttendanceDbContext dbContext,
            ILogger<CompanyForceCheckoutEmailJob> logger)
        {
            _invoker = invoker;
            _notifications = notifications;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Execute(PerformContext context)
        {
            var startedAt = DateTime.Now;
            var status = "Success";
            var details = "No employee force-checkout attempts were recorded during this run.";

            try
            {
                await _invoker.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                status = "Failed";
                details = $"The company force checkout job failed. Error: {ex.Message}";
                _logger.LogError(ex, "Company force checkout job failed.");
                throw;
            }
            finally
            {
                try
                {
                    var completedAt = DateTime.Now;
                    var report = await BuildCheckoutSummaryAsync(startedAt, completedAt);
                    details = string.IsNullOrWhiteSpace(report) ? details : report;

                    if (status == "Success" && details.Contains("Failed checkouts:", StringComparison.Ordinal))
                        status = "Partial";

                    await _notifications.NotifyAsync(
                        "CompanyForceCheckout",
                        "Company Force Checkout",
                        status,
                        startedAt,
                        completedAt,
                        details);
                }
                catch (Exception notificationException)
                {
                    _logger.LogError(notificationException,
                        "Failed to build or send Company Force Checkout summary email; original job result is preserved.");
                }
            }
        }

        private async Task<string> BuildCheckoutSummaryAsync(DateTime startedAt, DateTime completedAt)
        {
            var logs = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.CheckTime >= startedAt.AddDays(-1) &&
                    x.CheckTime <= completedAt &&
                    x.AttendanceState == HRMConstants.ForceCheckoutState)
                .OrderBy(x => x.CheckTime)
                .ToListAsync();

            if (logs.Count == 0)
                return "No employee force-checkout attempts were recorded during this run. No successful employee checkout details are available.";

            var biometricIds = logs
                .Select(x => x.EmployeeCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var employees = await _dbContext.EmployeeMappings
                .AsNoTracking()
                .Where(x => x.BiometricUserId != null && biometricIds.Contains(x.BiometricUserId))
                .ToListAsync();

            var employeeByBiometricId = employees
                .GroupBy(x => x.BiometricUserId!)
                .ToDictionary(x => x.Key, x => x.First());

            var successful = logs
                .Where(x => x.IsProcessed && x.Status == "Success")
                .GroupBy(x => x.EmployeeCode)
                .Select(x => x.OrderByDescending(y => y.CheckTime).First())
                .ToList();

            var failed = logs
                .Where(x => !x.IsProcessed || x.Status != "Success")
                .GroupBy(x => x.EmployeeCode)
                .Select(x => x.OrderByDescending(y => y.CheckTime).First())
                .ToList();

            var lines = new List<string>
            {
                $"Successful checkouts: {successful.Count}",
                $"Failed checkouts: {failed.Count}",
                string.Empty,
                "SUCCESSFUL CHECKOUTS"
            };

            foreach (var log in successful)
            {
                employeeByBiometricId.TryGetValue(log.EmployeeCode ?? string.Empty, out var employee);

                var previousCheckIn = logs
                    .Where(x => x.EmployeeCode == log.EmployeeCode &&
                                x.CheckTime < log.CheckTime &&
                                x.IsProcessed &&
                                x.Status == "Success" &&
                                (x.AttendanceState == HRMConstants.CheckInState ||
                                 x.AttendanceState == HRMConstants.ExtraCheckInState ||
                                 x.AttendanceState == HRMConstants.ManualCheckInState))
                    .OrderByDescending(x => x.CheckTime)
                    .FirstOrDefault();

                lines.Add($"Employee Name: {employee?.EmployeeName ?? "Unknown"}");
                lines.Add($"Employee Code: {employee?.HRMEmployeeCode ?? "Unknown"}");
                lines.Add($"Biometric Device ID: {log.EmployeeCode ?? "Not available"}");
                lines.Add($"Check-in Timestamp: {(previousCheckIn == null ? "Not available" : previousCheckIn.CheckTime.ToString("yyyy-MM-dd HH:mm:ss"))}");
                lines.Add($"Checkout Timestamp: {log.CheckTime:yyyy-MM-dd HH:mm:ss}");
                lines.Add($"Reason: {string.IsNullOrWhiteSpace(log.ErrorMessage) ? "Final company force checkout for employee outside active shift." : log.ErrorMessage}");
                lines.Add(new string('-', 36));
            }

            if (failed.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("FAILED CHECKOUTS");

                foreach (var log in failed)
                {
                    employeeByBiometricId.TryGetValue(log.EmployeeCode ?? string.Empty, out var employee);
                    lines.Add($"Employee Name: {employee?.EmployeeName ?? "Unknown"}");
                    lines.Add($"Employee Code: {employee?.HRMEmployeeCode ?? "Unknown"}");
                    lines.Add($"Biometric Device ID: {log.EmployeeCode ?? "Not available"}");
                    lines.Add($"Attempt Timestamp: {log.CheckTime:yyyy-MM-dd HH:mm:ss}");
                    lines.Add($"Reason: {string.IsNullOrWhiteSpace(log.ErrorMessage) ? "Checkout API attempt failed." : log.ErrorMessage}");
                    lines.Add(new string('-', 36));
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    public sealed class JobCompanyForceCheckoutInvoker
    {
        private readonly Zyra.LantimeServiceApp.JobService.JobCompanyForceCheckout _job;

        public JobCompanyForceCheckoutInvoker(Zyra.LantimeServiceApp.JobService.JobCompanyForceCheckout job) => _job = job;

        public Task ExecuteAsync(PerformContext context) => _job.Execute(context);
    }
}
