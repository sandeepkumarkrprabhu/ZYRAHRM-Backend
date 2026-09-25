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
using ZyraHangfireModels.ServiceModels;
using ZyraHangfireService.Cosntaants;

namespace ZyraHangfireService
{
    public class AttendanceJobService
    {
        private readonly ILogger<AttendanceJobService> _logger;
        private readonly ZyraIntegrationCredentials _credentials;
        private readonly IAttendanceDbService _attendanceDbService;
        private readonly IHttpService _httpService;
        private readonly AttendanceDbContext _dbcontext;


        public AttendanceJobService(AttendanceDbContext dbContext, IOptions<ZyraIntegrationCredentials> options, IAttendanceDbService attendanceDbService, IHttpService httpService, ILogger<AttendanceJobService> logger)
        {
            _dbcontext = dbContext;
            _credentials = options.Value;
            _attendanceDbService = attendanceDbService;
            _logger = logger;
            _httpService = httpService;
        }

        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(600)]
        public async Task SyncCheckInAttendanceAsync(PerformContext? context = null)
        {
            try
            {
                var lastSyncTime = GetLastSyncTime();

                _logger.LogInformation(
                    "Employee attendance check-in sync started at {Time}",
                    DateTime.Now);

                var records =
                    await _attendanceDbService.GetAttendanceAsync(lastSyncTime);

                _logger.LogInformation(
                    "Total employee records fetched from Biometric: {Count}",
                    records.Count);

                // ---------------------------------------------------------
                // 1. Load employee mappings once
                // ---------------------------------------------------------

                var biometricUserIds = records
                    .Select(x => x.EmployeeCode)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                _logger.LogInformation(
                    "Total unique biometric user IDs are: {Ids}",
                    string.Join(", ", biometricUserIds));

                var employeeMap =
                    await GetEmployeeMappingsAsync(biometricUserIds);

                // ---------------------------------------------------------
                // 2. Load attendance policies once
                // ---------------------------------------------------------

                var employeeIds = employeeMap.Values
                    .Select(x => x.UserId)
                    .Distinct()
                    .ToList();

                var policyMap =
                    await GetEmployeeAttendancePoliciesAsync(employeeIds);

                // ---------------------------------------------------------
                // 3. Process attendance records
                // ---------------------------------------------------------

                foreach (var record in records)
                {
                    try
                    {
                        await ProcessCheckInRecordAsync(
                            record,
                            employeeMap,
                            policyMap,
                            context);
                    }
                    catch (Exception ex)
                    {
                        var message =
                            $"Error processing attendance for {record.EmployeeName}";

                        _logger.LogError(ex, message);

                        context?.WriteLine(
                            ConsoleTextColor.Red,
                            $"{message}. Error: {ex.Message}");
                    }
                }

                // UpdateLastSyncTime(DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Attendance check-in sync failed");

                throw;
            }
        }

        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(600)] // 10 minutes lock
        public async Task SyncCheckOutAttendanceAsync(PerformContext? context = null)
        {
            try
            {
                var lastSyncTime = GetLastSyncTime();

                _logger.LogInformation(
                    "Employee attendance check-out sync started at {Time}",
                    DateTime.Now);

                var records =
                    await _attendanceDbService.GetAttendanceAsync(lastSyncTime);

                _logger.LogInformation(
                    "Total employee records fetched from Biometric: {Count}",
                    records.Count);

                // 1. Load employee mappings once
                var biometricUserIds = records
                    .Select(x => x.EmployeeCode)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                var employeeMap =
                    await GetEmployeeMappingsAsync(biometricUserIds);

                _logger.LogInformation(
                    "Total unique biometric user IDs are: {Count}. IDs: {Ids}",
                    biometricUserIds.Count,
                    string.Join(", ", biometricUserIds));

                // 2. Load attendance policies once
                var employeeIds = employeeMap.Values
                    .Select(x => x.UserId)
                    .Distinct()
                    .ToList();

                var policyMap =
                    await GetEmployeeAttendancePoliciesAsync(employeeIds);

                // 3. Process records
                foreach (var record in records)
                {
                    try
                    {
                        await ProcessCheckOutRecordAsync(
                            record,
                            employeeMap,
                            policyMap,
                            context);
                    }
                    catch (Exception ex)
                    {
                        var message =
                            $"Error processing check-out attendance for " +
                            $"{record.EmployeeName}";

                        _logger.LogError(ex, message);

                        context?.WriteLine(
                            ConsoleTextColor.Red,
                            $"{message}. Error: {ex.Message}");
                    }
                }

                // UpdateLastSyncTime(DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Attendance check-out sync failed");

                throw;
            }
        }

        private DateTime GetLastSyncTime()
        {
            return new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day); // Replace with DB value
            //return new DateTime(2026,03,02);
        }

        private async Task<bool> SaveAttendance(AttendanceAPIDto employeeAttendance, PerformContext? context)
        {
            var url = new Uri(new Uri("https://pc.pumexinfotech.com/"), "api/attendance/action");

            try
            {
                await _httpService.PostAsync(url.ToString(), employeeAttendance);
                return true;
            }
            catch (Exception ex)
            {
                var message =
                    $"Biometric attendance update failed for " +
                    $"{employeeAttendance.employee_code} dated " +
                    $"{employeeAttendance.date_time:dd-MM-yyyy HH:mm:ss}";

                            _logger.LogError(
                                ex,
                                "{Message}. Error: {Error}",
                                message,
                                ex.Message);

                            context?.WriteLine(
                                ConsoleTextColor.Red,
                                message);

                            return false;
            }
        }

        private async Task ProcessCheckInRecordAsync(AttendanceDto record, Dictionary<string, EmployeeMapperDto> employeeMap, Dictionary<int, EmployeeAttendancePolicy> policyMap, PerformContext? context)
        {
            // ---------------------------------------------------------
            // 1. Get employee mapping
            // ---------------------------------------------------------

            if (!employeeMap.TryGetValue(
                    record.EmployeeCode,
                    out var employee))
            {
                LogInformation(
                    context,
                    $"Biometric employee mapping not found for " +
                    $"{record.EmployeeName}. " +
                    $"Biometric code: {record.EmployeeCode}");

                return;
            }

            // ---------------------------------------------------------
            // 2. Get attendance policy
            // ---------------------------------------------------------

            if (!policyMap.TryGetValue(
                    employee.UserId,
                    out var attendancePolicy))
            {
                LogInformation(
                    context,
                    $"Check-in skipped for {record.EmployeeName}. " +
                    $"No attendance policy configured for employee.");

                return;
            }

            // ---------------------------------------------------------
            // 3. Build shift window
            // ---------------------------------------------------------

            var shiftWindow =
                BuildShiftWindow(
                    attendancePolicy,
                    record.CheckInTime,
                    record.EmployeeName);

            if (shiftWindow == null)
            {
                return;
            }

            // ---------------------------------------------------------
            // 4. Validate biometric check-in
            // ---------------------------------------------------------

            if (!IsValidCheckInTime(
                    record.CheckInTime,
                    shiftWindow,
                    record.EmployeeName,
                    context))
            {
                return;
            }

            // ---------------------------------------------------------
            // 5. Check duplicate check-in
            // ---------------------------------------------------------

            var alreadyCheckedIn =
                await IsAlreadyCheckedInAsync(
                    employee.EmployeeCode,
                    shiftWindow.Start,
                    shiftWindow.End);

            if (alreadyCheckedIn)
            {
                LogInformation(
                    context,
                    $"Check-in skipped for {record.EmployeeName}. " +
                    $"Employee has already checked in successfully. " +
                    $"Punch Time: {record.CheckInTime:dd-MM-yyyy HH:mm:ss}, " +
                    $"Shift Window: {shiftWindow.Start:dd-MM-yyyy HH:mm} - " +
                    $"{shiftWindow.End:dd-MM-yyyy HH:mm}");

                return;
            }

            // ---------------------------------------------------------
            // 6. Send attendance to ZYRA
            // ---------------------------------------------------------

            await SendCheckInAttendanceAsync(
                employee,
                record,
                shiftWindow,
                context);
        }

        private async Task ProcessCheckOutRecordAsync(AttendanceDto record, Dictionary<string, EmployeeMapperDto> employeeMap, Dictionary<int, EmployeeAttendancePolicy> policyMap, PerformContext? context)
        {
            // Employee mapping
            if (!employeeMap.TryGetValue(
                    record.EmployeeCode,
                    out var employee))
            {
                LogInformation(
                    context,
                    $"Biometric employee mapping not found for " +
                    $"{record.EmployeeName}. " +
                    $"Biometric code: {record.EmployeeCode}");

                return;
            }

            // Attendance policy
            if (!policyMap.TryGetValue(
                    employee.UserId,
                    out var attendancePolicy))
            {
                LogInformation(
                    context,
                    $"Check-out skipped for {record.EmployeeName}. " +
                    $"No attendance policy configured for employee.");

                return;
            }

            // Build shift window based on the employee's policy
            var shiftWindow =
                BuildShiftWindow(
                    attendancePolicy,
                    record.CheckOutTime,
                    record.EmployeeName);

            if (shiftWindow == null)
            {
                return;
            }

            // Validate checkout against shift end
            if (!IsValidCheckOutTime(
                    record.CheckOutTime,
                    shiftWindow,
                    record.EmployeeName,
                    context))
            {
                return;
            }

            await SendCheckOutAttendanceAsync(
                employee,
                record,
                shiftWindow,
                context);
        }

        private async Task<Dictionary<string, EmployeeMapperDto>> GetEmployeeMappingsAsync(IEnumerable<string> biometricUserIds)
        {
            var ids = biometricUserIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<string, EmployeeMapperDto>();
            }

            var employees = await _dbcontext.EmployeeMappings
                .AsNoTracking()
                .Where(x => ids.Contains(x.BiometricUserId))
                .Select(x => new EmployeeMapperDto
                {
                    UserId = x.Id,
                    EmployeeCode = x.HRMEmployeeCode,
                    EmployeeName = x.EmployeeName,
                    BiometricUserId = x.BiometricUserId
                })
                .ToListAsync();

            return employees
                .Where(x => !string.IsNullOrWhiteSpace(x.BiometricUserId))
                .GroupBy(x => x.BiometricUserId!)
                .ToDictionary(
                    x => x.Key,
                    x => x.First());
        }

        private async Task<Dictionary<int, EmployeeAttendancePolicy>> GetEmployeeAttendancePoliciesAsync(IEnumerable<int> employeeIds)
        {
            var ids = employeeIds
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<int, EmployeeAttendancePolicy>();
            }

            var policies = await _dbcontext.EmployeeAttendancePolicies
                .AsNoTracking()
                .Include(x => x.AttendancePolicy)
                    .ThenInclude(x => x.Rules)
                .Where(x =>
                    ids.Contains(x.EmployeeId) &&
                    x.IsEnabled &&
                    x.AttendancePolicy != null &&
                    x.AttendancePolicy.IsEnable)
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync();

            return policies
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(
                    x => x.Key,
                    x => x.First());
        }

        private ShiftWindow? BuildShiftWindow(EmployeeAttendancePolicy employeeShift, DateTime checkInTime, string employeeName)
        {
            var rules = employeeShift.AttendancePolicy?.Rules;

            var shiftStartValue = rules?
                .FirstOrDefault(x =>
                    x.RuleCode == HRMConstants.SHIFT_START_TIME_NAME)
                ?.RuleValue;

            var shiftEndValue = rules?
                .FirstOrDefault(x =>
                    x.RuleCode == HRMConstants.SHIFT_END_TIME_NAME)
                ?.RuleValue;

            if (!TimeSpan.TryParse(shiftStartValue, out var shiftStart))
            {
                _logger.LogWarning(
                    "Invalid shift start time for {EmployeeName}: {ShiftStart}",
                    employeeName,
                    shiftStartValue);

                return null;
            }

            if (!TimeSpan.TryParse(shiftEndValue, out var shiftEnd))
            {
                _logger.LogWarning(
                    "Invalid shift end time for {EmployeeName}: {ShiftEnd}",
                    employeeName,
                    shiftEndValue);

                return null;
            }

            // ---------------------------------------------------------
            // Determine overnight shift
            // ---------------------------------------------------------

            var isOvernightShift = shiftEnd <= shiftStart;

            var shiftEndTime = shiftEnd;

            if (isOvernightShift)
            {
                shiftEnd = shiftEnd.Add(TimeSpan.FromDays(1));
            }

            var shiftDate = checkInTime.Date;

            if (isOvernightShift &&
                checkInTime.TimeOfDay < shiftEndTime)
            {
                shiftDate = shiftDate.AddDays(-1);
            }

            // ---------------------------------------------------------
            // Grace period
            // ---------------------------------------------------------

            var gracePeriodValue = rules?
                .FirstOrDefault(x =>
                    x.RuleCode == HRMConstants.SHIFT_GRACE_PERIOD_NAME)
                ?.RuleValue;

            var gracePeriod = TimeSpan.Zero;

            if (!string.IsNullOrWhiteSpace(gracePeriodValue))
            {
                if (!TimeSpan.TryParse(
                        gracePeriodValue,
                        out gracePeriod))
                {
                    _logger.LogWarning(
                        "Invalid grace period for {EmployeeName}: {GracePeriod}. " +
                        "Using 00:00.",
                        employeeName,
                        gracePeriodValue);

                    gracePeriod = TimeSpan.Zero;
                }
            }

            var effectiveShiftStart =
                shiftStart.Subtract(gracePeriod);

            var effectiveShiftEnd =
                shiftEnd.Add(gracePeriod);

            return new ShiftWindow
            {
                // Check-in validation window
                Start = shiftDate.Add(shiftStart.Subtract(gracePeriod)),

                End = shiftDate.Add(shiftEnd.Add(gracePeriod)),

                // Actual shift times
                ShiftStart = shiftDate.Add(shiftStart),
                ShiftEnd = shiftDate.Add(shiftEnd),

                IsOvernight = isOvernightShift
            };
        }

        private bool IsValidCheckInTime(DateTime checkInTime, ShiftWindow shiftWindow, string employeeName, PerformContext? context)
        {
            if (checkInTime >= shiftWindow.Start &&
                checkInTime <= shiftWindow.End)
            {
                return true;
            }

            var message =
                $"Check-in skipped for {employeeName}. " +
                $"Check-in Time: {checkInTime:dd-MM-yyyy HH:mm:ss}, " +
                $"Valid Shift Window: " +
                $"{shiftWindow.Start:dd-MM-yyyy HH:mm} - " +
                $"{shiftWindow.End:dd-MM-yyyy HH:mm}";

            _logger.LogInformation(message);

            context?.WriteLine(
                ConsoleTextColor.Yellow,
                message);

            return false;
        }

        private bool IsValidCheckOutTime(DateTime checkOutTime, ShiftWindow shiftWindow, string employeeName, PerformContext? context)
        {
            if (checkOutTime >= shiftWindow.ShiftEnd &&
                checkOutTime <= shiftWindow.End)
            {
                return true;
            }

            var message =
                $"Check-out skipped for {employeeName}. " +
                $"Check-out Time: {checkOutTime:dd-MM-yyyy HH:mm:ss}, " +
                $"Valid Check-out Window: " +
                $"{shiftWindow.ShiftEnd:dd-MM-yyyy HH:mm} - " +
                $"{shiftWindow.End:dd-MM-yyyy HH:mm}";

            LogInformation(context, message);

            return false;
        }

        private async Task<bool> IsAlreadyCheckedInAsync(string employeeCode, DateTime shiftStart, DateTime shiftEnd)
        {
            return await _dbcontext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == employeeCode &&
                    x.Status == "Success" &&
                    x.CheckTime >= shiftStart &&
                    x.CheckTime <= shiftEnd);
        }

        private async Task SendCheckInAttendanceAsync(EmployeeMapperDto employee, AttendanceDto record, ShiftWindow shiftWindow, PerformContext? context)
        {
            var message =
                $"Attendance sync for employee {record.EmployeeName}, " +
                $"check-in: {record.CheckInTime:dd-MM-yyyy HH:mm:ss}, " +
                $"Shift Window: " +
                $"{shiftWindow.Start:dd-MM-yyyy HH:mm} - " +
                $"{shiftWindow.End:dd-MM-yyyy HH:mm}";

            _logger.LogInformation(message);
            context?.WriteLine(message);

            var request = new AttendanceAPIDto
            {
                employee_code = employee.EmployeeCode,
                type = "checkin",
                date_time = record.CheckInTime
            };

            var actionStatus =
                await SaveAttendance(request, context);

            if (actionStatus)
            {
                message =
                    $"Biometric attendance updated successfully " +
                    $"for {record.EmployeeName}";

                _logger.LogInformation(message);

                context?.WriteLine(
                    message,
                    ConsoleTextColor.DarkGreen);
            }
            else
            {
                message =
                    $"Biometric attendance update failed " +
                    $"for {record.EmployeeName}";

                _logger.LogWarning(message);

                context?.WriteLine(
                    ConsoleTextColor.White,
                    message);
            }
        }

        private async Task SendCheckOutAttendanceAsync(EmployeeMapperDto employee, AttendanceDto record, ShiftWindow shiftWindow, PerformContext? context)
        {
            var message =
                $"Attendance sync for employee {record.EmployeeName}, " +
                $"check-out: {record.CheckOutTime:dd-MM-yyyy HH:mm:ss}, " +
                $"Shift End: {shiftWindow.End:dd-MM-yyyy HH:mm}";

            _logger.LogInformation(message);
            context?.WriteLine(message);

            var request = new AttendanceAPIDto
            {
                employee_code = employee.EmployeeCode,
                type = "checkOut",
                date_time = record.CheckOutTime
            };

            var actionStatus =
                await SaveAttendance(request, context);

            if (actionStatus)
            {
                message =
                    $"Biometric check-out attendance updated successfully " +
                    $"for {record.EmployeeName}";

                _logger.LogInformation(message);

                context?.WriteLine(
                    message,
                    ConsoleTextColor.DarkGreen);
            }
            else
            {
                message =
                    $"Biometric check-out attendance update failed " +
                    $"for {record.EmployeeName}";

                _logger.LogWarning(message);

                context?.WriteLine(
                    ConsoleTextColor.DarkRed,
                    message);
            }
        }

        private void LogInformation(PerformContext? context, string message)
        {
            _logger.LogInformation(message);

            context?.WriteLine(
                ConsoleTextColor.Yellow,
                message);
        }

    }
}
