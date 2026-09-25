using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zyra.LantimeServiceApp.Constants;
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
        private readonly IAttendanceApiService _apiService;
        private readonly IExtraTimeCalculator _calculator;
        private readonly IShiftService _shiftService;
        private readonly ExtraTimeEvaluationSettings _settings;
        private readonly ILogger<ExtraTimeEvaluationService> _logger;

        public ExtraTimeEvaluationService(
            AttendanceDbContext dbContext,
            IAttendanceProvider attendanceProvider,
            IAttendanceApiService apiService,
            IExtraTimeCalculator calculator,
            IShiftService shiftService,
            IOptions<ExtraTimeEvaluationSettings> options,
            ILogger<ExtraTimeEvaluationService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _attendanceProvider = attendanceProvider ?? throw new ArgumentNullException(nameof(attendanceProvider));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _shiftService = shiftService ?? throw new ArgumentNullException(nameof(shiftService));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task EvaluateAsync(
            DateTime evaluationTime,
            PerformContext? context = null)
        {
            var workDates = new[]
            {
                evaluationTime.Date,
                evaluationTime.Date.AddDays(-1)
            };

            foreach (var workDate in workDates)
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
                    x.EffectiveFrom.Date <= workDate &&
                    x.EffectiveTo.Date >= workDate)
                .ToListAsync();

            if (assignments.Count == 0)
                return;

            var candidates = assignments
                .Select(x => BuildCandidate(x, workDate))
                .Where(x => x != null)
                .Select(x => x!)
                .Where(x => x.ShiftEnd.AddMinutes(
                    x.IsOvernight
                        ? _settings.OvernightShiftEvaluationDelayMinutes
                        : _settings.DayShiftEvaluationDelayMinutes) <= evaluationTime)
                .ToList();

            if (candidates.Count == 0)
                return;

            var from = candidates.Min(x => x.ShiftEnd);
            var to = candidates.Max(x =>
                x.ShiftEnd.AddMinutes(_settings.MaximumOvertimeMinutes));

            var punches = await _attendanceProvider.GetPunchesAsync(from, to);

            var punchesByEmployee = punches
                .Where(x => !string.IsNullOrWhiteSpace(x.EmployeeCode))
                .GroupBy(x => x.EmployeeCode)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(p => p.CheckTime).ToList());

            foreach (var candidate in candidates)
            {
                await EvaluateCandidateAsync(
                    candidate,
                    punchesByEmployee,
                    evaluationTime,
                    context);
            }
        }

        private ExtraTimeCandidate? BuildCandidate(
            EmployeeAttendancePolicy assignment,
            DateTime workDate)
        {
            var rules = assignment.AttendancePolicy?.Rules;

            if (rules == null)
                return null;

            var startValue = rules
                .FirstOrDefault(x => x.RuleCode == HRMConstants.SHIFT_START_TIME_NAME)
                ?.RuleValue;

            var endValue = rules
                .FirstOrDefault(x => x.RuleCode == HRMConstants.SHIFT_END_TIME_NAME)
                ?.RuleValue;

            if (!TimeSpan.TryParse(startValue, out var shiftStart) ||
                !TimeSpan.TryParse(endValue, out var shiftEnd))
            {
                _logger.LogWarning(
                    "Extra-time evaluation skipped for employee {EmployeeId}: invalid shift times.",
                    assignment.EmployeeId);

                return null;
            }

            var shift = _shiftService.BuildShiftWindow(
                assignment,
                workDate.AddHours(12),
                assignment.EmployeeMapping?.EmployeeName ?? "Unknown");

            if (shift == null)
                return null;

            return new ExtraTimeCandidate(
                assignment.EmployeeId,
                assignment.EmployeeMapping!,
                shift.ShiftEnd,
                shift.IsOvernight);
        }

        private async Task EvaluateCandidateAsync(
            ExtraTimeCandidate candidate,
            Dictionary<string, List<DateTime>> punchesByEmployee,
            DateTime evaluationTime,
            PerformContext? context)
        {
            if (string.IsNullOrWhiteSpace(candidate.Employee.BiometricUserId) ||
                string.IsNullOrWhiteSpace(candidate.Employee.HRMEmployeeCode))
                return;

            if (!punchesByEmployee.TryGetValue(
                    candidate.Employee.BiometricUserId,
                    out var punches))
                return;

            var interval = _calculator.Calculate(
                candidate.ShiftEnd,
                punches,
                evaluationTime,
                _settings.MaximumOvertimeMinutes);

            if (interval == null)
                return;

            var hasExtraCheckIn = await HasExtraLogAsync(
                candidate.Employee.BiometricUserId,
                interval.CheckInTime,
                ExtraCheckInState);

            if (!hasExtraCheckIn)
            {
                var success = await _apiService.SendAsync(new AttendanceAPIDto
                {
                    employee_code = candidate.Employee.HRMEmployeeCode,
                    type = "checkin",
                    date_time = interval.CheckInTime
                });

                await SaveLogAsync(
                    candidate.Employee.BiometricUserId,
                    interval.CheckInTime,
                    ExtraCheckInState,
                    success,
                    success
                        ? $"Extra time check-in. Extra minutes: {interval.DurationMinutes}."
                        : "Failed to create extra-time check-in.");

                if (!success)
                    return;
            }

            var hasExtraCheckOut = await HasExtraLogAsync(
                candidate.Employee.BiometricUserId,
                interval.CheckOutTime,
                ExtraCheckOutState);

            if (hasExtraCheckOut)
                return;

            var checkoutSuccess = await _apiService.SendAsync(new AttendanceAPIDto
            {
                employee_code = candidate.Employee.HRMEmployeeCode,
                type = "checkout",
                date_time = interval.CheckOutTime
            });

            await SaveLogAsync(
                candidate.Employee.BiometricUserId,
                interval.CheckOutTime,
                ExtraCheckOutState,
                checkoutSuccess,
                checkoutSuccess
                    ? $"Extra time checkout. Extra minutes: {interval.DurationMinutes}."
                    : "Failed to create extra-time check-out.");

            Log(
                context,
                $"Extra time {(checkoutSuccess ? "SUCCESS" : "FAILED")} for " +
                $"{candidate.Employee.EmployeeName}: " +
                $"{interval.CheckInTime:dd-MM-yyyy HH:mm} - " +
                $"{interval.CheckOutTime:dd-MM-yyyy HH:mm} " +
                $"({interval.DurationMinutes} minutes).");
        }

        private async Task<bool> HasExtraLogAsync(
            string biometricUserId,
            DateTime checkTime,
            string state)
        {
            return await _dbContext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == biometricUserId &&
                    x.IsProcessed &&
                    x.AttendanceState == state &&
                    x.CheckTime == checkTime);
        }

        private async Task SaveLogAsync(
            string employeeCode,
            DateTime checkTime,
            string state,
            bool success,
            string message)
        {
            _dbContext.AttendanceLogs.Add(new AttendanceLog
            {
                EmployeeCode = employeeCode,
                CheckTime = checkTime,
                AttendanceState = state,
                IsProcessed = success,
                ProcessedAt = DateTime.Now,
                Status = success ? "Success" : "Failed",
                ErrorMessage = message
            });

            await _dbContext.SaveChangesAsync();
        }

        private static void Log(PerformContext? context, string message)
        {
            context?.WriteLine(ConsoleTextColor.Cyan, message);
        }

        private sealed record ExtraTimeCandidate(
            int EmployeeId,
            EmployeeMapping Employee,
            DateTime ShiftEnd,
            bool IsOvernight);
    }
}