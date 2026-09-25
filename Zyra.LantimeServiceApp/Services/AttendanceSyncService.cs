using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;
using ZyraHangfireModels.ServiceModels;

namespace Zyra.LantimeServiceApp.Services
{
    public sealed class AttendanceSyncService : IAttendanceSyncService
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly IAttendanceDbService _attendanceDbService;
        private readonly IAttendanceApiService _attendanceApiService;
        private readonly IAttendanceLogService _attendanceLogService;
        private readonly IShiftService _shiftService;
        private readonly ILogger<AttendanceSyncService> _logger;

        public AttendanceSyncService(
            AttendanceDbContext dbContext,
            IAttendanceDbService attendanceDbService,
            IAttendanceApiService attendanceApiService,
            IAttendanceLogService attendanceLogService,
            IShiftService shiftService,
            ILogger<AttendanceSyncService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _attendanceDbService = attendanceDbService ?? throw new ArgumentNullException(nameof(attendanceDbService));
            _attendanceApiService = attendanceApiService ?? throw new ArgumentNullException(nameof(attendanceApiService));
            _attendanceLogService = attendanceLogService ?? throw new ArgumentNullException(nameof(attendanceLogService));
            _shiftService = shiftService ?? throw new ArgumentNullException(nameof(shiftService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SyncCheckInAsync(PerformContext? context = null)
        {
            await SyncAsync(false, context);
        }

        public async Task SyncCheckOutAsync(PerformContext? context = null)
        {
            await SyncAsync(true, context);
        }

        private async Task SyncAsync(bool checkout, PerformContext? context)
        {
            try
            {
                var syncTime = DateTime.Today;

                Log(context,
                    $"Employee attendance {(checkout ? "check-out" : "check-in")} sync started at {DateTime.Now}");

                var records = await _attendanceDbService.GetAttendanceAsync(syncTime);

                if (records == null || records.Count == 0)
                {
                    Log(context, "No biometric attendance records found.", ConsoleTextColor.Cyan);
                    return;
                }

                var biometricIds = records
                    .Select(x => x.EmployeeCode)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                var employeeMap = await GetEmployeeMappingsAsync(biometricIds);

                var employeeIds = employeeMap.Values
                    .Select(x => x.UserId)
                    .Distinct()
                    .ToList();

                var policyMap = await GetEmployeeAttendancePoliciesAsync(employeeIds);

                foreach (var record in records)
                {
                    try
                    {
                        if (checkout)
                            await ProcessCheckOutRecordAsync(record, employeeMap, policyMap, context);
                        else
                            await ProcessCheckInRecordAsync(record, employeeMap, policyMap, context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error processing attendance for {EmployeeName}",
                            record.EmployeeName);

                        Log(
                            context,
                            $"Error processing attendance for {record.EmployeeName}: {ex.Message}",
                            ConsoleTextColor.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Attendance {AttendanceType} sync failed",
                    checkout ? "check-out" : "check-in");

                throw;
            }
        }

        private async Task ProcessCheckInRecordAsync(
            AttendanceDto record,
            Dictionary<string, EmployeeMapperDto> employeeMap,
            Dictionary<int, EmployeeAttendancePolicy> policyMap,
            PerformContext? context)
        {
            if (!employeeMap.TryGetValue(record.EmployeeCode, out var employee))
            {
                Log(context,
                    $"Biometric employee mapping not found for {record.EmployeeName}. " +
                    $"Biometric code: {record.EmployeeCode}",
                    ConsoleTextColor.Red);
                return;
            }

            if (!policyMap.TryGetValue(employee.UserId, out var policy))
            {
                Log(context,
                    $"Check-in skipped for {record.EmployeeName}. No attendance policy configured.");
                return;
            }

            var shift = _shiftService.BuildShiftWindow(
                policy,
                record.CheckInTime,
                record.EmployeeName);

            if (shift == null || record.CheckInTime < shift.Start || record.CheckInTime > shift.End)
            {
                Log(context,
                    $"Check-in skipped for {record.EmployeeName}. " +
                    $"Check-in Time: {record.CheckInTime:dd-MM-yyyy HH:mm:ss}",
                    ConsoleTextColor.Yellow);
                return;
            }

            var alreadyCheckedIn = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == employee.EmployeeCode &&
                    x.Status == "Success" &&
                    x.CheckTime >= shift.Start &&
                    x.CheckTime <= shift.End);

            if (alreadyCheckedIn)
            {
                Log(context,
                    $"Check-in skipped for {record.EmployeeName}. Employee has already checked in successfully.");
                return;
            }

            var request = new AttendanceAPIDto
            {
                employee_code = employee.EmployeeCode,
                type = "checkin",
                date_time = record.CheckInTime
            };

            var success = await _attendanceApiService.SendAsync(request);

            await _attendanceLogService.LogAsync(
                record.EmployeeCode,
                request.date_time,
                success,
                request.type ?? "checkin");

            Log(
                context,
                $"Biometric check-in {(success ? "updated successfully" : "update failed")} for {record.EmployeeName}",
                success ? ConsoleTextColor.Green : ConsoleTextColor.Red);
        }

        private async Task ProcessCheckOutRecordAsync(
            AttendanceDto record,
            Dictionary<string, EmployeeMapperDto> employeeMap,
            Dictionary<int, EmployeeAttendancePolicy> policyMap,
            PerformContext? context)
        {
            if (!employeeMap.TryGetValue(record.EmployeeCode, out var employee))
            {
                Log(context,
                    $"Biometric employee mapping not found for {record.EmployeeName}. " +
                    $"Biometric code: {record.EmployeeCode}");
                return;
            }

            if (!policyMap.TryGetValue(employee.UserId, out var policy))
            {
                Log(context,
                    $"Check-out skipped for {record.EmployeeName}. No attendance policy configured.");
                return;
            }

            if (record.CheckOutTime == DateTime.MinValue)
                return;

            var shift = _shiftService.BuildShiftWindow(
                policy,
                record.CheckOutTime,
                record.EmployeeName);

            if (shift == null ||
                record.CheckOutTime < shift.ShiftEnd ||
                record.CheckOutTime > shift.End)
            {
                Log(context,
                    $"Check-out skipped for {record.EmployeeName}. " +
                    $"Check-out Time: {record.CheckOutTime:dd-MM-yyyy HH:mm:ss}",
                    ConsoleTextColor.Yellow);
                return;
            }

            var request = new AttendanceAPIDto
            {
                employee_code = employee.EmployeeCode,
                type = "checkOut",
                date_time = record.CheckOutTime
            };

            var success = await _attendanceApiService.SendAsync(request);

            await _attendanceLogService.LogAsync(
                record.EmployeeCode,
                request.date_time,
                success,
                request.type ?? "checkOut");

            Log(
                context,
                $"Biometric check-out {(success ? "updated successfully" : "update failed")} for {record.EmployeeName}",
                success ? ConsoleTextColor.Green : ConsoleTextColor.Red);
        }

        private async Task<Dictionary<string, EmployeeMapperDto>> GetEmployeeMappingsAsync(
            IEnumerable<string> biometricUserIds)
        {
            var ids = biometricUserIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<string, EmployeeMapperDto>();

            var employees = await _dbContext.EmployeeMappings
                .AsNoTracking()
                .Where(x =>
                    ids.Contains(x.BiometricUserId) &&
                    !x.IsExcludeFromBiometric &&
                    x.IsActive)
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
                .ToDictionary(x => x.Key, x => x.First());
        }

        private async Task<Dictionary<int, EmployeeAttendancePolicy>> GetEmployeeAttendancePoliciesAsync(
            IEnumerable<int> employeeIds)
        {
            var ids = employeeIds.Distinct().ToList();

            if (ids.Count == 0)
                return new Dictionary<int, EmployeeAttendancePolicy>();

            var policies = await _dbContext.EmployeeAttendancePolicies
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
                .ToDictionary(x => x.Key, x => x.First());
        }

        private void Log(
            PerformContext? context,
            string message,
            ConsoleTextColor color = ConsoleTextColor.Yellow)
        {
            _logger.LogInformation(message);
            context?.WriteLine(color, message);
        }
    }
}
