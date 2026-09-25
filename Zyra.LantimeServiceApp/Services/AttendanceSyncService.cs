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
        private readonly IAttendanceProvider _attendanceProvider;
        private readonly IAttendanceApiService _attendanceApiService;
        private readonly IAttendanceLogService _attendanceLogService;
        private readonly IShiftService _shiftService;
        private readonly ILogger<AttendanceSyncService> _logger;

        public AttendanceSyncService(
            AttendanceDbContext dbContext,
            IAttendanceProvider attendanceProvider,
            IAttendanceApiService attendanceApiService,
            IAttendanceLogService attendanceLogService,
            IShiftService shiftService,
            ILogger<AttendanceSyncService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _attendanceProvider = attendanceProvider ?? throw new ArgumentNullException(nameof(attendanceProvider));
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
                var today = DateTime.Today;

                Log(
                    context,
                    $"Employee attendance {(checkout ? "check-out" : "check-in")} sync started at {DateTime.Now}");

                // Raw punches are required because MIN/MAX grouped by calendar day
                // cannot correctly identify punches for overnight shifts.
                var punches = await _attendanceProvider.GetPunchesAsync(
                    today.AddDays(-1),
                    today.AddDays(2));

                if (punches.Count == 0)
                {
                    Log(context, "No biometric punches found.", ConsoleTextColor.Cyan);
                    return;
                }

                var biometricIds = punches
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

                foreach (var employee in employeeMap.Values)
                {
                    try
                    {
                        if (!policyMap.TryGetValue(employee.UserId, out var policy))
                        {
                            Log(
                                context,
                                $"{(checkout ? "Check-out" : "Check-in")} skipped for {employee.EmployeeName}. " +
                                "No attendance policy configured.");
                            continue;
                        }

                        var employeePunches = punches
                            .Where(x => x.EmployeeCode == employee.BiometricUserId)
                            .Select(x => x.CheckTime)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();

                        if (checkout)
                        {
                            await ProcessCheckOutAsync(
                                employee,
                                policy,
                                employeePunches,
                                today,
                                context);
                        }
                        else
                        {
                            await ProcessCheckInAsync(
                                employee,
                                policy,
                                employeePunches,
                                today,
                                context);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error processing attendance for Employee {EmployeeName}",
                            employee.EmployeeName);

                        Log(
                            context,
                            $"Error processing attendance for {employee.EmployeeName}: {ex.Message}",
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

        private async Task ProcessCheckInAsync(
            EmployeeMapperDto employee,
            EmployeeAttendancePolicy policy,
            IReadOnlyCollection<DateTime> punches,
            DateTime today,
            PerformContext? context)
        {
            foreach (var referenceDate in new[] { today, today.AddDays(-1) })
            {
                // Use noon so ShiftService resolves the calendar date correctly
                // for overnight shifts instead of treating midnight as the previous shift date.
                var shiftReferenceTime = referenceDate.AddHours(12);

                var shift = _shiftService.BuildShiftWindow(
                    policy,
                    shiftReferenceTime,
                    employee.EmployeeName);

                if (shift == null)
                    continue;

                var checkInTime = punches
                    .Where(x => x >= shift.Start && x < shift.ShiftEnd)
                    .OrderBy(x => x)
                    .FirstOrDefault();

                if (checkInTime == default)
                    continue;

                var alreadyProcessed = await _dbContext.AttendanceLogs
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.EmployeeCode == employee.BiometricUserId &&
                        x.IsProcessed &&
                        x.AttendanceState == "checkin" &&
                        x.CheckTime == checkInTime);

                if (alreadyProcessed)
                    continue;

                var request = new AttendanceAPIDto
                {
                    employee_code = employee.EmployeeCode,
                    type = "checkin",
                    date_time = checkInTime
                };

                var success = await _attendanceApiService.SendAsync(request);

                await _attendanceLogService.LogAsync(
                    employee.BiometricUserId,
                    checkInTime,
                    success,
                    "checkin");

                Log(
                    context,
                    $"Biometric check-in {(success ? "updated successfully" : "update failed")} " +
                    $"for {employee.EmployeeName} at {checkInTime:dd-MM-yyyy HH:mm:ss}",
                    success ? ConsoleTextColor.Green : ConsoleTextColor.Red);

                if (success)
                    return;
            }
        }

        private async Task ProcessCheckOutAsync(
            EmployeeMapperDto employee,
            EmployeeAttendancePolicy policy,
            IReadOnlyCollection<DateTime> punches,
            DateTime today,
            PerformContext? context)
        {
            var candidateCheckouts = new List<(ShiftWindow Shift, DateTime CheckOutTime)>();

            foreach (var referenceDate in new[] { today, today.AddDays(-1) })
            {
                // Use noon so overnight shifts are resolved against the intended
                // calendar date rather than midnight being treated as the prior shift date.
                var shiftReferenceTime = referenceDate.AddHours(12);

                var shift = _shiftService.BuildShiftWindow(
                    policy,
                    shiftReferenceTime,
                    employee.EmployeeName);

                if (shift == null)
                    continue;

                var checkOutTime = punches
                    .Where(x => x >= shift.ShiftEnd && x <= shift.End)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                if (checkOutTime != default)
                    candidateCheckouts.Add((shift, checkOutTime));
            }

            var candidate = candidateCheckouts
                .OrderByDescending(x => x.CheckOutTime)
                .FirstOrDefault();

            if (candidate.CheckOutTime == default)
                return;

            var alreadyProcessed = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == employee.BiometricUserId &&
                    x.IsProcessed &&
                    (x.AttendanceState == "checkout" ||
                     x.AttendanceState == "checkOut") &&
                    x.CheckTime == candidate.CheckOutTime);

            if (alreadyProcessed)
                return;

            var request = new AttendanceAPIDto
            {
                employee_code = employee.EmployeeCode,
                type = "checkOut",
                date_time = candidate.CheckOutTime
            };

            var success = await _attendanceApiService.SendAsync(request);

            await _attendanceLogService.LogAsync(
                employee.BiometricUserId,
                candidate.CheckOutTime,
                success,
                "checkout");

            Log(
                context,
                $"Biometric check-out {(success ? "updated successfully" : "update failed")} " +
                $"for {employee.EmployeeName} at {candidate.CheckOutTime:dd-MM-yyyy HH:mm:ss}",
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
