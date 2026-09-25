using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;

namespace Zyra.LantimeServiceApp.JobService
{
    public class JobEmployeeBioPunchTimeUpdate : IEmployeePunchSyncJob
    {
        private readonly ILogger<JobEmployeeBioPunchTimeUpdate> _logger;
        private readonly IJobService _jobService;
        private readonly IAttendanceProvider _attendanceProvider;
        private readonly IEmployeePunchProcessor _processor;
        private readonly IEmployeeService _employeeService;

        public JobEmployeeBioPunchTimeUpdate(
            ILogger<JobEmployeeBioPunchTimeUpdate> logger,
            IJobService jobService,
            IAttendanceProvider attendanceProvider,
            IEmployeePunchProcessor processor,
            IEmployeeService employeeService)
        {
            _logger = logger;
            _jobService = jobService;
            _attendanceProvider = attendanceProvider;
            _processor = processor;
            _employeeService = employeeService;
        }

        public async Task Execute(PerformContext context)
        {
            try
            {
                var lastSyncTime = _jobService.GetLastSyncTime();

                _logger.LogInformation("Punch Sync started at {Time}", DateTime.Now);
                context.WriteLine(ConsoleTextColor.Cyan, "Punch sync started...");

                // -----------------------------
                // STEP 1: DATA FROM PROVIDER
                // -----------------------------
                var employees = await _employeeService.GetActiveEmployees();
                var records = await _attendanceProvider.GetLastPunchDataAsync();

                if (employees == null || records == null)
                {
                    _logger.LogWarning("No data found for punch sync");
                    return;
                }

                var recordMap = records
                    .Where(r => !string.IsNullOrEmpty(r.BiometricUserId))
                    .ToDictionary(x => x.BiometricUserId);

                // -----------------------------
                // STEP 2: PROCESS
                // -----------------------------
                foreach (var emp in employees)
                {
                    if (string.IsNullOrEmpty(emp.BiometricUserId))
                        continue;

                    if (!recordMap.TryGetValue(emp.BiometricUserId, out var biometric))
                    {
                        _logger.LogWarning(
                            "No punch found for {Employee} ({Device})",
                            emp.EmployeeName,
                            emp.BiometricUserId);

                        continue;
                    }

                    await _processor.ProcessAsync(emp, biometric);

                    context.WriteLine(
                        ConsoleTextColor.DarkYellow,
                        $"Updated punch → {emp.EmployeeName}");
                }

                _logger.LogInformation("Punch Sync completed successfully");
                context.WriteLine(ConsoleTextColor.Green, "Punch sync completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Punch Sync failed");
                context.WriteLine(ConsoleTextColor.Red, ex.Message);
                throw;
            }
        }
    }
}
