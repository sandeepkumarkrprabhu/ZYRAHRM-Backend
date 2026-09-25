
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using ZYRA.Attendance.Infrastructure;

namespace ZyraHangfireService
{
    public class EmployeeBioPunchTimeUpdateService
    {
        private readonly ILogger<EmployeeBioPunchTimeUpdateService> _logger;
        private readonly AttendanceDbContext _context;
        private readonly IJobService _jobService;
        private readonly IAttendanceDbService _attendanceDbService;

        public EmployeeBioPunchTimeUpdateService(ILogger<EmployeeBioPunchTimeUpdateService> logger, IJobService jobService, AttendanceDbContext context, IAttendanceDbService attendanceDbService)
        {
            _context = context;
            _logger = logger;
            _jobService = jobService;
            _attendanceDbService = attendanceDbService;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SyncEmployeePunchCheckoutTime(PerformContext context)
        {
            try
            {
                context.WriteLine("Current TimeZone: ", TimeZoneInfo.Local);

                var lastSyncTime = _jobService.GetLastSyncTime();
                var message = $"Employee last punch time sync (Check-Out) started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                _logger.LogInformation(message);
                context.WriteLine(ConsoleTextColor.Black, message);

                var employees = await _context.EmployeeMappings
                                              .Where(e => e.IsActive && !e.IsExcludeFromBiometric)
                                              .ToListAsync();

                // Fetching the attendance for the provided date
                var records = await _attendanceDbService.GetLastPunchTime();

                foreach (var employee in employees)
                {
                    bool updateStatus = false;
                    message = "";
                    var employeeAttendance = records.Where(p => p.BiometricUserId == employee.BiometricUserId).FirstOrDefault();
                    if (employeeAttendance != null)
                    {
                        var selectedEmployee = _context.EmployeeMappings.Where(p => p.Id == employee.Id).FirstOrDefault();
                        if (selectedEmployee != null)
                        {
                            selectedEmployee.LatestCheckoutFromBiometric = employeeAttendance.LatestCheckoutFromBiometric;
                            selectedEmployee.UpdatedUser = "Job";
                            selectedEmployee.UpdatedDateTime = DateTime.Now;
                            _context.SaveChanges();
                            updateStatus = true;
                            message = $"Employee : device id: {employee.BiometricUserId}, Name {employee.EmployeeName}, punch time updated from biometric.";
                        }
                    }
                    else
                    {
                        updateStatus = false;
                        message = $"Employee device id {employee.BiometricUserId}, Name : {employee.EmployeeName}, punch time missing from biometric.";
                    }
                    if (!string.IsNullOrEmpty(message))
                    {
                        var colorMode = updateStatus ? ConsoleTextColor.DarkYellow : ConsoleTextColor.Red;
                        _logger.LogInformation(message);
                        context.WriteLine(colorMode, message);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
