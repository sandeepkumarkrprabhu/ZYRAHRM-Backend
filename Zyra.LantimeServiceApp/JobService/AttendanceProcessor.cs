using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public class AttendanceProcessor : IAttendanceProcessor
    {
        private readonly IAttendancePolicy _policy;
        private readonly IAttendanceApiService _apiService;
        private readonly IAttendanceLogService _logService;
        private readonly ILogger<AttendanceProcessor> _logger;

        public AttendanceProcessor(
            IAttendancePolicy policy,
            IAttendanceApiService apiService,
            IAttendanceLogService logService,
            ILogger<AttendanceProcessor> logger)
        {
            _policy = policy;
            _apiService = apiService;
            _logService = logService;
            _logger = logger;
        }

        public async Task ProcessAsync(
            EmployeeMapping employee,
            AttendanceDto attendance,
            PerformContext context,
            TimeSpan timeSpan)
        {
            try
            {
                // -------------------------------
                // VALIDATION
                // -------------------------------
                if (string.IsNullOrEmpty(employee.HRMEmployeeCode))
                {
                    var msg = $"Skipped: Missing HRM code for {employee.EmployeeName}";
                    _logger.LogWarning(msg);
                    context.WriteLine(ConsoleTextColor.Yellow, msg);
                    return;
                }

                // -------------------------------
                // STEP 1: APPLY BUSINESS POLICY
                // -------------------------------
                var request = _policy.BuildRequest(employee, attendance, timeSpan);

                if (request == null)
                {
                    var msg = $"No attendance action required for {employee.EmployeeName}";
                    _logger.LogInformation(msg);
                    context.WriteLine(ConsoleTextColor.DarkYellow, msg);
                    return;
                }

                // -------------------------------
                // STEP 2: CALL HRMS API
                // -------------------------------
                var result = await _apiService.SendAsync(request);

                // -------------------------------
                // STEP 3: LOG RESULT
                // -------------------------------
                var statusText = result ? "SUCCESS" : "FAILED";

                var message =
                    $"Attendance {request.type} {statusText} for {employee.EmployeeName}";

                _logger.LogInformation(message);

                context.WriteLine(
                    result ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                    message);

                // -------------------------------
                // STEP 4: SAVE DB LOG
                // -------------------------------
                // The policy has already selected the exact operation timestamp.
                // Keep the logger free of business rules and record that timestamp directly.
                await _logService.LogAsync(
                    attendance.EmployeeCode,
                    request.date_time,
                    result,
                    request.type ?? string.Empty);
            }
            catch (Exception ex)
            {
                var error = $"Error processing attendance for {employee.EmployeeName}";

                _logger.LogError(ex, error);
                context.WriteLine(ConsoleTextColor.Red, error);

                throw;
            }
        }
    }
}
