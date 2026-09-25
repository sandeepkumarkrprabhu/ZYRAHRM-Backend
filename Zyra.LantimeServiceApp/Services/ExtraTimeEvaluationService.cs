using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Services
{
    public sealed class ExtraTimeEvaluationService : IExtraTimeEvaluationService
    {
        private const string ExtraCheckInState = "Extra checkin";
        private const string ExtraCheckOutState = "Extra checkout";

        private readonly AttendanceDbContext _dbContext;
        private readonly IAttendanceProvider _attendanceProvider;
        private readonly IAttendanceApiService _attendanceApiService;
        private readonly IShiftService _shiftService;
        private readonly IExtraTimeCalculator _calculator;
        private readonly ExtraTimeEvaluationSettings _settings;
        private readonly ILogger<ExtraTimeEvaluationService> _logger;

        public ExtraTimeEvaluationService(
            AttendanceDbContext dbContext,
            IAttendanceProvider attendanceProvider,
            IAttendanceApiService attendanceApiService,
            IShiftService shiftService,
            IExtraTimeCalculator calculator,
            IOptions<ExtraTimeEvaluationSettings> options,
            ILogger<ExtraTimeEvaluationService> logger)
        {
            _dbContext = dbContext;
            _attendanceProvider = attendanceProvider;
            _attendanceApiService = attendanceApiService;
            _shiftService = shiftService;
            _calculator = calculator;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task EvaluateAsync(
            DateTime evaluationTime,
            PerformContext? context = null)
        {
            // Evaluate both today and yesterday so an overnight shift can
            // finish on the following calendar day.
            foreach (var workDate in new[]
            {
                evaluationTime.Date,
                evaluationTime.Date.AddDays(-1)
            })
            {
                await EvaluateWorkDateAsync(workDate, evaluationTime, context);
            }
        }

        private async Task EvaluateWorkDateAsync(
            DateTime workDate,
            DateTime evaluationTime,
            PerformContext? context)
        {
            var assignments = await _dbContext.EmployeeAttendancePolicies
                .AsNoTracking()
                .Include(x => x.AttendancePolicy)
                    .ThenInclude(x => x.Rules)
                .Include(x => x.EmployeeMapping)
                .Where(x =>
                    x.IsEnabled &&
                    x.AttendancePolicy != null &&
                    x.AttendancePolicy.IsEnable &&
                    x.EmployeeMapping != null &&
                    x.EmployeeMapping.IsActive &&
                    !x.EmployeeMapping.IsExcludeFromBiometric &&
                    x.EffectiveFrom <= workDate.AddDays(1).AddTicks(-1) &&
                    x.EffectiveTo >= workDate)
                .ToListAsync();

            if (assignments.Count == 0)
                return;

            var candidates = assignments
                .Select(assignment =>
                {
                    var employee = assignment.EmployeeMapping;

                    if (employee == null ||
                        string.IsNullOrWhiteSpace(employee.BiometricUserId) ||
                        string.IsNullOrWhiteSpace(employee.HRMEmployeeCode))
                    {
                        return null;
                    }

                    var shift = _shiftService.BuildShiftWindow(
                        assignment,
                        workDate.AddHours(12),
                        employee.EmployeeName ?? "Unknown");

                    if (shift == null)
                        return null;

                    var delayMinutes = shift.IsOvernight
                        ? _settings.OvernightShiftEvaluationDelayMinutes
                        : _settings.DayShiftEvaluationDelayMinutes;

                    if (shift.ShiftEnd.AddMinutes(delayMinutes) > evaluationTime)
                        return null;

                    return new ExtraTimeCandidate(
                        employee,
                        shift.ShiftEnd);
                })
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            if (candidates.Count == 0)
                return;

            // One biometric query for the whole work-date batch instead of
            // one query per employee.
            var from = candidates.Min(x => x.ShiftEnd);
            var to = candidates.Max(x =>
                x.ShiftEnd.AddMinutes(_settings.MaximumOvertimeMinutes));

            var punches = await _attendanceProvider.GetPunchesAsync(from, to);

            foreach (var candidate in candidates)
            {
                await EvaluateEmployeeAsync(
                    candidate,
                    punches,
                    evaluationTime,
                    context);
            }
        }

        private async Task EvaluateEmployeeAsync(
            ExtraTimeCandidate candidate,
            IEnumerable<BiometricPunch> punches,
            DateTime evaluationTime,
            PerformContext? context)
        {
            var employeePunches = punches
                .Where(x => x.EmployeeCode == candidate.Employee.BiometricUserId)
                .Select(x => x.CheckTime);

            var interval = _calculator.Calculate(
                candidate.ShiftEnd,
                employeePunches,
                evaluationTime,
                _settings.MaximumOvertimeMinutes);

            if (interval == null)
                return;

            var checkInLogged = await HasSuccessfulExtraLogAsync(
                candidate.Employee.BiometricUserId!,
                interval.CheckInTime,
                ExtraCheckInState);

            if (!checkInLogged)
            {
                var success = await _attendanceApiService.SendAsync(
                    new AttendanceAPIDto
                    {
                        employee_code = candidate.Employee.HRMEmployeeCode,
                        type = "checkin",
                        date_time = interval.CheckInTime
                    });

                await SaveLogAsync(
                    candidate.Employee.BiometricUserId!,
                    interval.CheckInTime,
                    ExtraCheckInState,
                    success,
                    success
                        ? $"Extra-time check-in. Duration: {interval.DurationMinutes} minutes."
                        : "Failed to create extra-time check-in.");

                if (!success)
                    return;
            }

            var checkoutLogged = await HasSuccessfulExtraLogAsync(
                candidate.Employee.BiometricUserId!,
                interval.CheckOutTime,
                ExtraCheckOutState);

            if (!checkoutLogged)
            {
                var success = await _attendanceApiService.SendAsync(
                    new AttendanceAPIDto
                    {
                        employee_code = candidate.Employee.HRMEmployeeCode,
                        type = "checkout",
                        date_time = interval.CheckOutTime
                    });

                await SaveLogAsync(
                    candidate.Employee.BiometricUserId!,
                    interval.CheckOutTime,
                    ExtraCheckOutState,
                    success,
                    success
                        ? $"Extra-time check-out. Duration: {interval.DurationMinutes} minutes."
                        : "Failed to create extra-time check-out.");
            }

            Log(
                context,
                $"Extra time processed for {candidate.Employee.EmployeeName}: " +
                $"{interval.CheckInTime:dd-MM-yyyy HH:mm} - " +
                $"{interval.CheckOutTime:dd-MM-yyyy HH:mm} " +
                $"({interval.DurationMinutes} minutes).");
        }

        private async Task<bool> HasSuccessfulExtraLogAsync(
            string employeeCode,
            DateTime checkTime,
            string attendanceState)
        {
            return await _dbContext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == employeeCode &&
                    x.IsProcessed &&
                    x.AttendanceState == attendanceState &&
                    x.CheckTime == checkTime);
        }

        private async Task SaveLogAsync(
            string employeeCode,
            DateTime checkTime,
            string attendanceState,
            bool success,
            string message)
        {
            _dbContext.AttendanceLogs.Add(new AttendanceLog
            {
                EmployeeCode = employeeCode,
                CheckTime = checkTime,
                AttendanceState = attendanceState,
                IsProcessed = success,
                ProcessedAt = DateTime.Now,
                Status = success ? "Success" : "Failed",
                ErrorMessage = message
            });

            await _dbContext.SaveChangesAsync();
        }

        private static void Log(
            PerformContext? context,
            string message)
        {
            context?.WriteLine(ConsoleTextColor.Cyan, message);
        }
    }
}