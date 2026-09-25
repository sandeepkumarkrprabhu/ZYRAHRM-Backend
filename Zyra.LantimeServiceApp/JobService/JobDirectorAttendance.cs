using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public class JobDirectorAttendance : IDirectorAttendanceJob
    {
        private readonly IEmployeeService _employeeService;
        private readonly IAttendanceApiService _apiService;
        private readonly IJobService _jobService;
        private readonly ILogger<JobDirectorAttendance> _logger;

        public JobDirectorAttendance(
            IEmployeeService employeeService,
            IAttendanceApiService apiService,
            IJobService jobService,
            ILogger<JobDirectorAttendance> logger)
        {
            _employeeService = employeeService;
            _apiService = apiService;
            _jobService = jobService;
            _logger = logger;
        }

        public async Task Execute(PerformContext context)
        {
            try
            {
                var lastSync = _jobService.GetLastSyncTime();

                _logger.LogInformation("DirectorAttendanceJob started at {Time}", DateTime.Now);
                context.WriteLine(ConsoleTextColor.Cyan, "Director attendance job started...");

                // -----------------------------
                // STEP 1: GET DIRECTORS
                // -----------------------------
                var directors = await _employeeService.GetDirectors();

                if (directors == null || !directors.Any())
                {
                    _logger.LogWarning("No directors found for attendance job");
                    return;
                }

                // -----------------------------
                // STEP 2: FIXED TIMINGS
                // -----------------------------
                var checkInTime = lastSync.Date; // 00:00 AM
                var checkOutTime = lastSync.Date.AddHours(8).AddMinutes(30);

                // -----------------------------
                // STEP 3: PROCESS DIRECTORS
                // -----------------------------
                foreach (var director in directors)
                {
                    if (string.IsNullOrEmpty(director.HRMEmployeeCode))
                    {
                        var msg = $"Skipped director {director.EmployeeName} (missing HRM code)";
                        _logger.LogWarning(msg);
                        context.WriteLine(ConsoleTextColor.Yellow, msg);
                        continue;
                    }

                    // -------------------------
                    // CHECK-IN
                    // -------------------------
                    var checkInRequest = new AttendanceAPIDto
                    {
                        employee_code = director.HRMEmployeeCode,
                        type = "checkin",
                        date_time = checkInTime
                    };

                    var inResult = await _apiService.SendAsync(checkInRequest);

                    var inStatus = inResult ? "SUCCESS" : "FAILED";

                    var inMessage =
                        $"Director CHECKIN {inStatus} for {director.EmployeeName} at {checkInTime}";

                    _logger.LogInformation(inMessage);
                    context.WriteLine(
                        inResult ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                        inMessage);

                    // -------------------------
                    // CHECK-OUT
                    // -------------------------
                    var checkOutRequest = new AttendanceAPIDto
                    {
                        employee_code = director.HRMEmployeeCode,
                        type = "checkout",
                        date_time = checkOutTime
                    };

                    var outResult = await _apiService.SendAsync(checkOutRequest);

                    var outStatus = outResult ? "SUCCESS" : "FAILED";

                    var outMessage =
                        $"Director CHECKOUT {outStatus} for {director.EmployeeName} at {checkOutTime}";

                    _logger.LogInformation(outMessage);
                    context.WriteLine(
                        outResult ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                        outMessage);
                }

                _logger.LogInformation("DirectorAttendanceJob completed successfully");
                context.WriteLine(ConsoleTextColor.Green, "Director attendance job completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DirectorAttendanceJob failed");
                context.WriteLine(ConsoleTextColor.Red, ex.Message);
                throw;
            }
        }
    }
}
