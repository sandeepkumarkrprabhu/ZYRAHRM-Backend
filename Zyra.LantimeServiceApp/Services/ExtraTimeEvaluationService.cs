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
using ZyraHangfireModels.ServiceModels;

namespace Zyra.LantimeServiceApp.Services
{
    public sealed class ExtraTimeEvaluationService : IExtraTimeEvaluationService
    {
        private const string ExtraCheckInState = "Extra checkin";
        private const string ExtraCheckOutState = "Extra checkout";

        private readonly AttendanceDbContext _dbContext;
        private readonly IAttendanceProvider _attendanceProvider;
        private readonly IAttendanceApiService _apiService;
        private readonly ExtraTimeEvaluationSettings _settings;
        private readonly ILogger<ExtraTimeEvaluationService> _logger;

        public ExtraTimeEvaluationService(
            AttendanceDbContext dbContext,
            IAttendanceProvider attendanceProvider,
            IAttendanceApiService apiService,
            IOptions<ExtraTimeEvaluationSettings> options,
            ILogger<ExtraTimeEvaluationService> logger)
        {
            _dbContext = dbContext;
            _attendanceProvider = attendanceProvider;
            _apiService = apiService;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task EvaluateAsync(
            DateTime evaluationTime,
            PerformContext? context = null)
        {
            var workDates = new[] { evaluationTime.Date, evaluationTime.Date.AddDays(-1) };

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
                .Where(x => x != null && x.ShiftEnd.AddMinutes(x.IsOvernight
                    ? _settings.OvernightShiftEvaluationDelayMinutes
                    : _settings.DayShiftEvaluationDelayMinutes) <= evaluationTime)
                .Select(x => x!)
                .ToList();

            if (candidates.Count == 0)
                return;

            // The biometric provider returns one calendar date at a time.
            // For overnight shifts we therefore need the following calendar
            // date as well because the logical work date remains workDate.
            var attendanceByDate = new Dictionary<DateTime, List<AttendanceDto>>();

            foreach (var date in candidates
                .SelectMany(x => x.IsOvernight
                    ? new[] { x.WorkDate, x.WorkDate.AddDays(1) }
                    : new[] { x.WorkDate })
                .Distinct())
            {
                attendanceByDate[date] =
                    await _attendanceProvider.GetAttendanceAsync(date);
            }

            foreach (var candidate in candidates)
            {
                await EvaluateCandidateAsync(
                    candidate,
                    attendanceByDate,
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

            var overnight = shiftEnd <= shiftStart;
            var actualShiftStart = workDate.Add(shiftStart);
            var actualShiftEnd = workDate.Add(shiftEnd);

            if (overnight)
                actualShiftEnd = actualShiftEnd.AddDays(1);

            return new ExtraTimeCandidate(
                assignment.EmployeeId,
                assignment.EmployeeMapping!,
                assignment.AttendancePolicy!,
                workDate,
                actualShiftStart,
                actualShiftEnd,
                overnight);
        }

        private async Task EvaluateCandidateAsync(
            ExtraTimeCandidate candidate,
            Dictionary<DateTime, List<AttendanceDto>> attendanceByDate,
            DateTime evaluationTime,
            PerformContext? context)
        {
            var punches = attendanceByDate
                .Where(x => candidate.IsOvernight
                    ? x.Key == candidate.WorkDate ||
                      x.Key == candidate.WorkDate.AddDays(1)
                    : x.Key == candidate.WorkDate)
                .SelectMany(x => x.Value)
                .SelectMany(x => GetPunchTimes(x))
                .Where(x => x > candidate.ShiftEnd)
                .Where(x => x <= candidate.ShiftEnd.AddMinutes(_settings.MaximumOvertimeMinutes))
                .Where(x => x <= evaluationTime)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();

            var lastPunch = punches.FirstOrDefault();

            if (lastPunch == default)
            {
                Log(context,
                    $"No extra time for {candidate.Employee.EmployeeName}. " +
                    $"Shift ended at {candidate.ShiftEnd:dd-MM-yyyy HH:mm}.");
                return;
            }

            if (await HasExtraTimeAlreadyProcessedAsync(
                    candidate.Employee.BiometricUserId,
                    candidate.ShiftEnd,
                    lastPunch))
            {
                Log(context,
                    $"Extra time already processed for {candidate.Employee.EmployeeName}: " +
                    $"{candidate.ShiftEnd:HH:mm} - {lastPunch:HH:mm}.");
                return;
            }

            var extraMinutes = (int)(lastPunch - candidate.ShiftEnd).TotalMinutes;

            if (extraMinutes <= 0)
                return;

            var checkInSuccess = await _apiService.SendAsync(
                new AttendanceAPIDto
                {
                    employee_code = candidate.Employee.HRMEmployeeCode,
                    type = "checkin",
                    date_time = candidate.ShiftEnd
                });

            if (!checkInSuccess)
            {
                await SaveLogAsync(
                    candidate.Employee.BiometricUserId,
                    candidate.ShiftEnd,
                    ExtraCheckInState,
                    false,
                    "Failed to create extra-time check-in.");

                return;
            }

            await SaveLogAsync(
                candidate.Employee.BiometricUserId,
                candidate.ShiftEnd,
                ExtraCheckInState,
                true,
                $"Extra time check-in. Extra minutes: {extraMinutes}.");

            var checkOutSuccess = await _apiService.SendAsync(
                new AttendanceAPIDto
                {
                    employee_code = candidate.Employee.HRMEmployeeCode,
                    type = "checkout",
                    date_time = lastPunch
                });

            await SaveLogAsync(
                candidate.Employee.BiometricUserId,
                lastPunch,
                ExtraCheckOutState,
                checkOutSuccess,
                checkOutSuccess
                    ? $"Extra time checkout. Extra minutes: {extraMinutes}."
                    : "Failed to create extra-time check-out.");

            Log(
                context,
                $"Extra time {(checkOutSuccess ? "SUCCESS" : "FAILED")} for " +
                $"{candidate.Employee.EmployeeName}: " +
                $"{candidate.ShiftEnd:dd-MM-yyyy HH:mm} - " +
                $"{lastPunch:dd-MM-yyyy HH:mm} ({extraMinutes} minutes).");
        }

        private static IEnumerable<DateTime> GetPunchTimes(AttendanceDto attendance)
        {
            yield return attendance.CheckInTime;

            if (attendance.CheckOutTime != DateTime.MinValue)
                yield return attendance.CheckOutTime;
        }

        private async Task<bool> HasExtraTimeAlreadyProcessedAsync(
            string biometricUserId,
            DateTime extraStart,
            DateTime extraEnd)
        {
            return await _dbContext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == biometricUserId &&
                    x.IsProcessed &&
                    x.AttendanceState == ExtraCheckOutState &&
                    x.CheckTime == extraEnd &&
                    x.ErrorMessage != null &&
                    x.ErrorMessage.Contains("Extra time checkout"));
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

        private static void Log(
            PerformContext? context,
            string message)
        {
            context?.WriteLine(ConsoleTextColor.Cyan, message);
        }

        private sealed record ExtraTimeCandidate(
            int EmployeeId,
            EmployeeMapping Employee,
            AttendancePolicyMaster Policy,
            DateTime WorkDate,
            DateTime ShiftStart,
            DateTime ShiftEnd,
            bool IsOvernight);
    }
}
