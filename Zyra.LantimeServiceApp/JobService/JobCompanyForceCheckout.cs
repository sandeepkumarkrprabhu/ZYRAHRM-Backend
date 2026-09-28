using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public sealed class JobCompanyForceCheckout : ICompanyForceCheckoutJob
    {
        private const string CheckInState = "checkin";
        private const string CheckoutState = "checkout";
        private const string AutoCheckoutState = "Auto checkout";
        private const string ForceCheckoutState = "Force checkout";
        private const string ExtraCheckInState = "Extra checkin";
        private const string ExtraCheckOutState = "Extra checkout";

        private readonly IAttendanceProvider _attendanceProvider;
        private readonly IAttendanceApiService _apiService;
        private readonly AttendanceDbContext _dbContext;
        private readonly IEmployeeAttendanceService _employeeAttendanceService;
        private readonly IShiftService _shiftService;
        private readonly ILogger<JobCompanyForceCheckout> _logger;

        public JobCompanyForceCheckout(
            IAttendanceProvider attendanceProvider,
            IAttendanceApiService apiService,
            AttendanceDbContext dbContext,
            IEmployeeAttendanceService employeeAttendanceService,
            IShiftService shiftService,
            ILogger<JobCompanyForceCheckout> logger)
        {
            _attendanceProvider = attendanceProvider;
            _apiService = apiService;
            _dbContext = dbContext;
            _employeeAttendanceService = employeeAttendanceService;
            _shiftService = shiftService;
            _logger = logger;
        }

        public async Task Execute(PerformContext context)
        {
            var now = DateTime.Now;

            try
            {
                _logger.LogInformation(
                    "Company force checkout job started at {Time}", now);

                context.WriteLine(
                    ConsoleTextColor.Cyan,
                    $"Company force checkout started at {now:yyyy-MM-dd HH:mm:ss}");

                // Process employees who have attendance activity in the current
                // or previous calendar day. We intentionally do not restrict this
                // to currently open records because an employee may have already
                // received a normal/policy checkout and then worked additional time.
                var employeeIds = await GetAttendanceEmployeeIdsAsync(now);

                if (employeeIds.Count == 0)
                {
                    context.WriteLine(
                        ConsoleTextColor.Gray,
                        "No attendance activity found.");
                    return;
                }

                var policyMap = await _employeeAttendanceService
                    .GetEmployeeAttendancePoliciesAsync(employeeIds);

                foreach (var employeeId in employeeIds)
                {
                    await ProcessEmployeeAsync(
                        employeeId,
                        policyMap,
                        now,
                        context);
                }

                _logger.LogInformation(
                    "Company force checkout job completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Company force checkout job failed.");

                context.WriteLine(
                    ConsoleTextColor.Red,
                    $"Company force checkout failed: {ex.Message}");

                throw;
            }
        }

        private async Task ProcessEmployeeAsync(
            int employeeId,
            IReadOnlyDictionary<int, EmployeeAttendancePolicy> policyMap,
            DateTime now,
            PerformContext context)
        {
            if (!policyMap.TryGetValue(employeeId, out var assignment) ||
                assignment.AttendancePolicy == null)
            {
                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"Force checkout skipped for employee {employeeId}: no active attendance policy.");
                return;
            }

            var employee = await _dbContext.EmployeeMappings
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == employeeId &&
                    x.IsActive &&
                    !x.IsExcludeFromBiometric);

            if (employee == null ||
                string.IsNullOrWhiteSpace(employee.BiometricUserId) ||
                string.IsNullOrWhiteSpace(employee.HRMEmployeeCode))
            {
                return;
            }

            var shift = _shiftService.BuildShiftWindow(
                assignment,
                now,
                employee.EmployeeName);

            if (shift == null)
            {
                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"Force checkout skipped for {employee.EmployeeName}: unable to determine shift.");
                return;
            }

            // Never force-close an employee while the assigned shift is active.
            // This protects overnight employees whose shift crosses midnight.
            if (now >= shift.ShiftStart && now <= shift.ShiftEnd)
            {
                context.WriteLine(
                    ConsoleTextColor.Yellow,
                    $"Force checkout excluded {employee.EmployeeName}: " +
                    $"active shift {shift.ShiftStart:yyyy-MM-dd HH:mm} - " +
                    $"{shift.ShiftEnd:yyyy-MM-dd HH:mm}");
                return;
            }

            var logs = await GetEmployeeAttendanceLogsAsync(
                employee.BiometricUserId,
                now);

            var latestCheckIn = logs
                .Where(x => x.AttendanceState == CheckInState)
                .OrderByDescending(x => x.CheckTime)
                .FirstOrDefault();

            if (latestCheckIn == null)
                return;

            var latestCheckout = logs
                .Where(x =>
                    x.AttendanceState == CheckoutState ||
                    x.AttendanceState == AutoCheckoutState ||
                    x.AttendanceState == ForceCheckoutState)
                .OrderByDescending(x => x.CheckTime)
                .FirstOrDefault();

            // ------------------------------------------------------------
            // CASE 1: The employee already checked out.
            //
            // Check the biometric device for a punch after that checkout.
            // If one exists, preserve the period from the last checkout to
            // the latest punch as an extra working session.
            // ------------------------------------------------------------
            if (latestCheckout != null &&
                latestCheckout.CheckTime >= latestCheckIn.CheckTime)
            {
                await ProcessExtraWorkingTimeAsync(
                    employee,
                    latestCheckout.CheckTime,
                    now,
                    context);

                return;
            }

            // ------------------------------------------------------------
            // CASE 2: The employee still has an open attendance.
            //
            // Do one final biometric lookup before using the company force
            // checkout time. A late device punch always wins.
            // ------------------------------------------------------------
            var latestPunch = await _attendanceProvider.GetLatestPunchAfterAsync(
                employee.BiometricUserId,
                latestCheckIn.CheckTime,
                now);

            var checkoutTime = latestPunch ?? now;
            var checkoutState = latestPunch.HasValue
                ? CheckoutState
                : ForceCheckoutState;

            await ProcessCheckoutAsync(
                employee,
                checkoutTime,
                checkoutState,
                latestPunch.HasValue
                    ? "Latest biometric punch used by company force checkout job"
                    : "Company force checkout",
                context);
        }

        private async Task ProcessExtraWorkingTimeAsync(
            EmployeeMapping employee,
            DateTime lastCheckoutTime,
            DateTime now,
            PerformContext context)
        {
            var latestPunch = await _attendanceProvider.GetLatestPunchAfterAsync(
                employee.BiometricUserId,
                lastCheckoutTime,
                now);

            if (!latestPunch.HasValue ||
                latestPunch.Value <= lastCheckoutTime)
            {
                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"No extra working time for {employee.EmployeeName} " +
                    $"after checkout {lastCheckoutTime:yyyy-MM-dd HH:mm:ss}.");
                return;
            }

            var extraCheckoutTime = latestPunch.Value;
            var extraMinutes = (int)(extraCheckoutTime - lastCheckoutTime).TotalMinutes;

            if (extraMinutes <= 0)
                return;

            // The existing extra-time model represents additional work as:
            //   Extra checkin  = previous checkout
            //   Extra checkout = latest biometric punch
            //
            // This makes the additional period visible to the existing
            // ExtraTimeEvaluationService without changing the base attendance
            // session that was already closed.
            var hasExtraCheckIn = await HasAttendanceLogAsync(
                employee.BiometricUserId,
                lastCheckoutTime,
                ExtraCheckInState);

            if (!hasExtraCheckIn)
            {
                var checkInSuccess = await _apiService.SendAsync(
                    new AttendanceAPIDto
                    {
                        employee_code = employee.HRMEmployeeCode,
                        type = CheckInState,
                        date_time = lastCheckoutTime
                    });

                await SaveAttendanceLogAsync(
                    employee.BiometricUserId,
                    lastCheckoutTime,
                    ExtraCheckInState,
                    checkInSuccess,
                    checkInSuccess
                        ? $"Company force checkout created extra-time check-in. Extra minutes: {extraMinutes}."
                        : "Failed to create company force checkout extra-time check-in.");

                if (!checkInSuccess)
                    return;
            }

            var hasExtraCheckOut = await HasAttendanceLogAsync(
                employee.BiometricUserId,
                extraCheckoutTime,
                ExtraCheckOutState);

            if (hasExtraCheckOut)
            {
                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"Extra working time already processed for {employee.EmployeeName}: " +
                    $"{lastCheckoutTime:HH:mm} - {extraCheckoutTime:HH:mm}.");
                return;
            }

            var checkOutSuccess = await _apiService.SendAsync(
                new AttendanceAPIDto
                {
                    employee_code = employee.HRMEmployeeCode,
                    type = CheckoutState,
                    date_time = extraCheckoutTime
                });

            await SaveAttendanceLogAsync(
                employee.BiometricUserId,
                extraCheckoutTime,
                ExtraCheckOutState,
                checkOutSuccess,
                checkOutSuccess
                    ? $"Company force checkout created extra-time check-out. Extra minutes: {extraMinutes}."
                    : "Failed to create company force checkout extra-time check-out.");

            context.WriteLine(
                checkOutSuccess ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                $"Extra working time {(checkOutSuccess ? "SUCCESS" : "FAILED")} " +
                $"for {employee.EmployeeName}: " +
                $"{lastCheckoutTime:dd-MM-yyyy HH:mm} - " +
                $"{extraCheckoutTime:dd-MM-yyyy HH:mm} " +
                $"({extraMinutes} minutes).");
        }

        private async Task ProcessCheckoutAsync(
            EmployeeMapping employee,
            DateTime checkoutTime,
            string checkoutState,
            string message,
            PerformContext context)
        {
            var result = await _apiService.SendAsync(
                new AttendanceAPIDto
                {
                    employee_code = employee.HRMEmployeeCode,
                    type = CheckoutState,
                    date_time = checkoutTime
                });

            if (result)
            {
                await SaveAttendanceLogAsync(
                    employee.BiometricUserId,
                    checkoutTime,
                    checkoutState,
                    true,
                    message);
            }

            context.WriteLine(
                result ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                $"{checkoutState} {(result ? "SUCCESS" : "FAILED")} " +
                $"for {employee.EmployeeName} at {checkoutTime:yyyy-MM-dd HH:mm:ss}");
        }

        private async Task<List<AttendanceLog>> GetEmployeeAttendanceLogsAsync(
            string biometricUserId,
            DateTime now)
        {
            return await _dbContext.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.EmployeeCode == biometricUserId &&
                    x.CheckTime >= now.Date.AddDays(-1) &&
                    x.CheckTime <= now &&
                    x.IsProcessed &&
                    x.Status == "Success" &&
                    (x.AttendanceState == CheckInState ||
                     x.AttendanceState == CheckoutState ||
                     x.AttendanceState == AutoCheckoutState ||
                     x.AttendanceState == ForceCheckoutState))
                .OrderByDescending(x => x.CheckTime)
                .ToListAsync();
        }

        private async Task<List<int>> GetAttendanceEmployeeIdsAsync(DateTime now)
        {
            var employeeCodes = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.CheckTime >= now.Date.AddDays(-1) &&
                    x.CheckTime <= now &&
                    x.IsProcessed &&
                    x.Status == "Success" &&
                    (x.AttendanceState == CheckInState ||
                     x.AttendanceState == CheckoutState ||
                     x.AttendanceState == AutoCheckoutState ||
                     x.AttendanceState == ForceCheckoutState))
                .Select(x => x.EmployeeCode)
                .Distinct()
                .ToListAsync();

            return await _dbContext.EmployeeMappings
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    !x.IsExcludeFromBiometric &&
                    employeeCodes.Contains(x.BiometricUserId))
                .Select(x => x.Id)
                .Distinct()
                .ToListAsync();
        }

        private async Task<bool> HasAttendanceLogAsync(
            string biometricUserId,
            DateTime checkTime,
            string state)
        {
            return await _dbContext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == biometricUserId &&
                    x.CheckTime == checkTime &&
                    x.AttendanceState == state &&
                    x.IsProcessed &&
                    x.Status == "Success");
        }

        private async Task SaveAttendanceLogAsync(
            string biometricUserId,
            DateTime checkTime,
            string state,
            bool success,
            string message)
        {
            _dbContext.AttendanceLogs.Add(new AttendanceLog
            {
                EmployeeCode = biometricUserId,
                CheckTime = checkTime,
                AttendanceState = state,
                IsProcessed = success,
                ProcessedAt = DateTime.Now,
                Status = success ? "Success" : "Failed",
                ErrorMessage = message
            });

            await _dbContext.SaveChangesAsync();
        }
    }
}
