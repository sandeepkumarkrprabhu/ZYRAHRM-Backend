using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace ZyraHangfireService
{
    public class AttendanceJobService_old
    {
        private readonly ILogger<AttendanceJobService_old> _logger;
        private readonly ZyraIntegrationCredentials _credentials;
        private readonly AttendanceDbContext _context;
        private readonly IAttendanceDbService _attendanceDbService;
        private readonly IJobService _jobService;
        private readonly IHttpService _httpService;

        public AttendanceJobService_old(
            IOptions<ZyraIntegrationCredentials> options,
            IAttendanceDbService attendanceDbService,
            AttendanceDbContext context,
            IHttpService httpService,
            IJobService jobService,
            ILogger<AttendanceJobService_old> logger)
        {
            _credentials = options.Value;
            _attendanceDbService = attendanceDbService;
            _logger = logger;
            _context = context;
            _jobService = jobService;
            _httpService = httpService;
        }

        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(600)] // 10 minutes lock
        public async Task SyncEmployeeAttendance(PerformContext context)
        {
            try
            {
                context.WriteLine("Current TimeZone: ", TimeZoneInfo.Local);

                var lastSyncTime = _jobService.GetLastSyncTime();
                var message = $"Employee Attendnace sync started at {DateTime.Now}";
                _logger.LogInformation(message);
                context.WriteLine(ConsoleTextColor.Black, message);

                // Fetching the attendance for the provided date
                var records = await _attendanceDbService.GetAttendanceAsync(lastSyncTime);

                // Fetching the employee mapping from the db
                var employees = await GetEmployees();

                if (employees != null)
                {
                    // process normal employees 
                    foreach (var employee in employees)
                    {
                        var employeeAttendance = records.Where(p => p.EmployeeCode == employee.BiometricUserId).FirstOrDefault();
                        if (employeeAttendance != null)
                        {
                            message = $"Attendnace sync for the employee {employee.EmployeeName}, checkin,  {employeeAttendance.CheckInTime}";
                            _logger.LogInformation(message);
                            context.WriteLine(ConsoleTextColor.White, message);
                            var apibodyRequest = GetEmployeeAttendanceAPIBody(employeeAttendance, employee.HRMEmployeeCode);

                            if (apibodyRequest == null)
                            {
                                message = $"Skipped attendnace sync for the employee {employee.EmployeeName}";
                                _logger.LogWarning(message);
                                context.WriteLine(ConsoleTextColor.DarkYellow, message);
                                continue;
                            }
                            var actionStatus = await SaveAttendance(apibodyRequest, context);
                            var status = (actionStatus ? "successfully " : "failed ");
                            var attendanceStatus = apibodyRequest.type ?? "";
                            message = $"Biometric attendance updated {status} for {employeeAttendance.EmployeeName}";
                            _logger.LogInformation(message);
                            context.WriteLine(actionStatus ? ConsoleTextColor.DarkGreen : ConsoleTextColor.Red, message);

                            LogAttendance(employeeAttendance, actionStatus,attendanceStatus);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attendance sync failed");
                context.WriteLine(ConsoleTextColor.Red, ex.Message);
                throw;
            }
        }

        private async Task<IEnumerable<EmployeeMapping>> GetAllEmployees()
        {
            return await _context.EmployeeMappings.Where(emp => emp.IsActive &&
                                !emp.IsExcludeFromBiometric).ToListAsync();
        }

        private async Task<IEnumerable<EmployeeMapping>> GetEmployees()
        {
            var todayStart = DateTime.Today;
            var tomorrowStart = todayStart.AddDays(1);
            var now = DateTime.Now;

            // Before or at 3 PM → return all employees not checked in
            if (now.Hour <= 15)
            {
                return await _context.EmployeeMappings
                            .Where(emp => emp.IsActive &&
                                !emp.IsExcludeFromBiometric &&
                                !_context.AttendanceLogs.Any(log =>
                                    log.EmployeeCode == emp.BiometricUserId &&
                                    log.IsProcessed &&
                                    log.AttendanceState == "checkin" &&
                                    log.CheckTime >= todayStart &&
                                    log.CheckTime < tomorrowStart))
                            .ToListAsync();
            }

            // After 3 PM → return last 1 hour data
            var oneHourAgo = now.AddHours(-1);

            return await _context.EmployeeMappings
                .Where(f => f.IsActive && !f.IsExcludeFromBiometric &&
                            f.LastCheckoutFinal != null &&
                            f.LastCheckoutFinal >= oneHourAgo &&
                            f.LastCheckoutFinal <= now.AddMinutes(5))
                .ToListAsync();
        }

        public async Task ProcessEmployeeExtraHours(PerformContext context)
        {
            var lastSyncTime = _jobService.GetLastSyncTime();

            var employees = await GetAllEmployees();
            var employeeAttRecords = await _attendanceDbService.GetAttendanceAsync(lastSyncTime);

            foreach (var employee in employees)
            {
                if (string.IsNullOrEmpty(employee.BiometricUserId))
                    continue;

                // Get latest checkout from Biometric Device puch records
                var finalCheckout = employeeAttRecords
                    .Where(f => f.EmployeeCode == employee.BiometricUserId)
                    .OrderByDescending(f => f.CheckOutTime)
                    .FirstOrDefault();

                // Get last processed attendance log from DB
                var attendanceLastUpdate = await _context.AttendanceLogs
                    .Where(f => f.EmployeeCode == employee.BiometricUserId && f.IsProcessed)
                    .OrderByDescending(f => f.CheckTime)
                    .FirstOrDefaultAsync();

                if (attendanceLastUpdate == null || finalCheckout == null)
                    continue;

                var difference = finalCheckout.CheckOutTime - attendanceLastUpdate.CheckTime;

                if (difference.TotalMinutes > 0)
                {
                    // Extra working time
                    var extraHours = difference.TotalHours;
                    // Example: log or store
                    Console.WriteLine($"Employee {employee.BiometricUserId} worked extra: {extraHours} hours");
                    // Write code for save the checkin and checkout and log the details.

                }
            }
        }

        // Auto force Checkout when employee missed to update the checkout attendance
        public async Task AutoCheckoutAllEmployees(PerformContext context)
        {
            try
            {
                var lastSyncTime = _jobService.GetLastSyncTime();
                var message = $"Auto checkout (force) started at {DateTime.Now}";
                _logger.LogInformation(message);
                context.WriteLine(ConsoleTextColor.Cyan, message);

                var employees = await _context.EmployeeMappings
                                              .Where(e => e.IsActive && !e.IsExcludeFromBiometric)
                                              .ToListAsync();

                var autoCheckoutTime = lastSyncTime.AddHours(23).AddMinutes(59);

                foreach (var employee in employees)
                {
                    if (string.IsNullOrEmpty(employee.HRMEmployeeCode))
                    {
                        message = $"Auto checkout for {employee.EmployeeName} skipped. please check the employee master data. ";

                        _logger.LogInformation(message);
                        context.WriteLine(ConsoleTextColor.Red, message);
                        continue;
                    }

                    var apiBody = new AttendanceAPIDto
                    {
                        employee_code = employee.HRMEmployeeCode,
                        type = "checkout",
                        date_time = autoCheckoutTime
                    };

                    var result = await SaveAttendance(apiBody, context);
                    
                    var status = result ? "SUCCESS" : "FAILED";
                    message = $"Auto checkout {status} for {employee.EmployeeName}";

                    _logger.LogInformation(message);
                    context.WriteLine(result ? ConsoleTextColor.Green : ConsoleTextColor.Red, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto checkout failed");
                context.WriteLine(ConsoleTextColor.Red, ex.Message);
                throw;
            }
        }

        public async Task AutoAttendanceForBusinessDirectors(PerformContext context)
        {

            try
            {
                var lastSyncTime = _jobService.GetLastSyncTime();
                var message = $"Business Directors auto checkin checkout started at {DateTime.Now}";
                _logger.LogInformation(message);
                context.WriteLine(ConsoleTextColor.Cyan, message);

                var employees = await _context.EmployeeMappings
                                              .Where(e => e.IsExcludeFromBiometric)
                                              .ToListAsync();

                var autoCheckInTime = lastSyncTime.AddHours(00).AddMinutes(00);
                var autoCheckoutTime = lastSyncTime.AddHours(08).AddMinutes(30);

                foreach (var employee in employees)
                {
                    var apiCheckInBody = new AttendanceAPIDto
                    {
                        employee_code = employee.HRMEmployeeCode,
                        type = "checkin",
                        date_time = autoCheckInTime
                    };

                    var result = await SaveAttendance(apiCheckInBody, context);

                    var status = result ? "SUCCESS" : "FAILED";
                    message = $"Directors checkin : {autoCheckInTime} update : {status} for {employee.EmployeeName}";

                    _logger.LogInformation(message);
                    context.WriteLine(result ? ConsoleTextColor.Green : ConsoleTextColor.Red, message);

                    var apiCheckoutBody = new AttendanceAPIDto
                    {
                        employee_code = employee.HRMEmployeeCode,
                        type = "checkout",
                        date_time = autoCheckoutTime
                    };

                    result = await SaveAttendance(apiCheckoutBody, context);

                    status = result ? "SUCCESS" : "FAILED";
                    message = $"Directors checkout : { autoCheckoutTime } update {status} for {employee.EmployeeName}";

                    _logger.LogInformation(message);
                    context.WriteLine(result ? ConsoleTextColor.Green : ConsoleTextColor.Red, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto checkout failed");
                context.WriteLine(ConsoleTextColor.Red, ex.Message);
                throw;
            }
        }

        #region Helper Attendance 
        private void LogAttendance(AttendanceDto employeeAttendance, bool actionStatus, string attendanceStatus)
        {
            var time = employeeAttendance.CheckInTime.TimeOfDay;

            var attendanceLog = new AttendanceLog
            {
                EmployeeCode = employeeAttendance.EmployeeCode,
                IsProcessed = actionStatus,
                ProcessedAt = DateTime.Now,
                Status = actionStatus ? "Success" : "Failed",
                AttendanceState = attendanceStatus??"",
                CheckTime = (time <= new TimeSpan(15, 0, 0) ? employeeAttendance.CheckInTime : employeeAttendance.CheckOutTime)
            };
            _context.AttendanceLogs.Add(attendanceLog);
            _context.SaveChanges();
        }

        private AttendanceAPIDto? GetEmployeeAttendanceAPIBody(AttendanceDto employeeAttendance, string empCode)
        {
            var now = DateTime.Now.TimeOfDay;

            // Morning → ONLY Check-in
            if (now >= new TimeSpan(8, 0, 0) && now <= new TimeSpan(15, 0, 0))
            {
                if (employeeAttendance.CheckInTime != DateTime.MinValue)
                {
                    return new AttendanceAPIDto
                    {
                        employee_code = empCode,
                        type = "checkin",
                        date_time = employeeAttendance.CheckInTime
                    };
                }
            }

            // Evening → ONLY Check-out (but ONLY if valid)
            else if (now >= new TimeSpan(18, 0, 0) && now <= new TimeSpan(23, 0, 0))
            {
                // Only allow checkout if it is logically valid
                if (employeeAttendance.CheckOutTime != DateTime.MinValue &&
                    employeeAttendance.CheckInTime != DateTime.MinValue &&
                    employeeAttendance.CheckOutTime > employeeAttendance.CheckInTime)
                {
                    return new AttendanceAPIDto
                    {
                        employee_code = empCode,
                        type = "checkout",
                        date_time = employeeAttendance.CheckOutTime
                    };
                }
            }

            return null;
        }

        private async Task<bool> SaveAttendance(AttendanceAPIDto employeeAttendance, PerformContext context)
        {
            context.WriteLine("Api Url: ", _credentials.Domain);
            var url = new Uri(new Uri("https://pc.pumexinfotech.com/"), "api/attendance/action");
            context.WriteLine(ConsoleTextColor.Black, $"Url for update Attendance: {url}");

            try
            {
                await _httpService.PostAsync(url.ToString(), employeeAttendance);
                return true;
            }
            catch (Exception ex)
            {
                var message = $"Biometric save attendance failed for {employeeAttendance.employee_code} dated {employeeAttendance.date_time}";
                message = message + ", Error : " + ex.StackTrace;
                _logger.LogError(message);
                context.WriteLine(ConsoleTextColor.Red, message);
                return false;
            }
        }
        #endregion
    }
}
