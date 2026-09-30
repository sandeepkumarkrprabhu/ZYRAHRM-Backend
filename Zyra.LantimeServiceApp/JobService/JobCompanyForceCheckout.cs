using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Constants;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public sealed class JobCompanyForceCheckout : ICompanyForceCheckoutJob
    {
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
                Log(
                    context,
                    ConsoleTextColor.Cyan,
                    "Company force checkout job started at {0:yyyy-MM-dd HH:mm:ss}",
                    now);

                // IMPORTANT:
                // Shift exclusion is the first filter. We do not query attendance
                // logs or biometric punches until we know the employee is outside
                // the current applicable shift.
                var employees = await GetEmployeesOutsideActiveShiftAsync(
                    now,
                    context);

                if (employees.Count == 0)
                {
                    Log(
                        context,
                        ConsoleTextColor.Gray,
                        "No employees are outside their active shift.");

                    return;
                }

                Log(
                    context,
                    ConsoleTextColor.Cyan,
                    "Found {0} employees requiring company force checkout processing.",
                    employees.Count);

                var failedEmployees = new List<ForceCheckoutEmployee>();

                foreach (var employee in employees)
                {
                    var result = await ProcessEmployeeAsync(
                        employee,
                        now,
                        context);

                    if (!result.Success)
                    {
                        failedEmployees.Add(new ForceCheckoutEmployee
                        {
                            Employee = employee,
                            Reason = result.FailureReason ?? "Attendance processing failed."
                        });
                    }
                }

                // Force checkout is the final fallback. It must run only after
                // every employee has gone through reconciliation/normal checkout.
                if (failedEmployees.Count > 0)
                {
                    Log(
                        context,
                        ConsoleTextColor.Yellow,
                        "Starting force checkout fallback for {0} failed employees.",
                        failedEmployees.Count);

                    await ForceCheckoutFailedEmployeesAsync(
                        failedEmployees,
                        now,
                        context);
                }
                else
                {
                    Log(
                        context,
                        ConsoleTextColor.Green,
                        "No failed employees. Force checkout fallback was not required.");
                }

                Log(
                    context,
                    ConsoleTextColor.Green,
                    "Company force checkout job completed. Processed={0}, Failed={1}",
                    employees.Count,
                    failedEmployees.Count);
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

        private async Task<List<EmployeeMapping>> GetEmployeesOutsideActiveShiftAsync(
            DateTime now,
            PerformContext context)
        {
            // Get every active, non-excluded employee. Biometric participation
            // is not an eligibility condition for company force checkout.
            var employees = await _dbContext.EmployeeMappings
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    !x.IsExcludeFromBiometric &&
                    x.HRMEmployeeCode != null)
                .ToListAsync();

            if (employees.Count == 0)
                return new List<EmployeeMapping>();

            var employeeIds = employees
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            var policyMap = await _employeeAttendanceService
                .GetEmployeeAttendancePoliciesAsync(employeeIds);

            var result = new List<EmployeeMapping>();

            foreach (var employee in employees)
            {
                if (!policyMap.TryGetValue(employee.Id, out var assignment) ||
                    assignment.AttendancePolicy == null)
                {
                    Log(
                        context,
                        ConsoleTextColor.Yellow,
                        "Employee selected: {0}; no active shift assignment, so company force checkout processing will continue.",
                        employee.EmployeeName);

                    result.Add(employee);
                    continue;
                }

                var shift = _shiftService.BuildShiftWindow(
                    assignment,
                    now,
                    employee.EmployeeName);

                if (shift == null)
                {
                    Log(
                        context,
                        ConsoleTextColor.Yellow,
                        "Employee selected: {0}; current shift could not be determined, so company force checkout processing will continue.",
                        employee.EmployeeName);

                    result.Add(employee);
                    continue;
                }

                // The only exclusion is when the current time is inside the
                // employee's actual shift window. BuildShiftWindow handles
                // overnight shifts.
                if (now >= shift.ShiftStart && now <= shift.ShiftEnd)
                {
                    Log(
                        context,
                        ConsoleTextColor.Gray,
                        "Shift exclusion: {0} is inside active shift {1:yyyy-MM-dd HH:mm} - {2:yyyy-MM-dd HH:mm}.",
                        employee.EmployeeName,
                        shift.ShiftStart,
                        shift.ShiftEnd);

                    continue;
                }

                result.Add(employee);

                Log(
                    context,
                    ConsoleTextColor.Yellow,
                    "Employee selected: {0}; outside shift {1:yyyy-MM-dd HH:mm} - {2:yyyy-MM-dd HH:mm}.",
                    employee.EmployeeName,
                    shift.ShiftStart,
                    shift.ShiftEnd);
            }

            return result;
        }

        private async Task<ProcessResult> ProcessEmployeeAsync(
            EmployeeMapping employee,
            DateTime now,
            PerformContext context)
        {
            try
            {
                // Attendance reconciliation requires a biometric id. Employees
                // without one are still processed and will go through direct
                // force checkout below.
                var logs = !string.IsNullOrWhiteSpace(employee.BiometricUserId)
                    ? await GetEmployeeAttendanceLogsAsync(employee.BiometricUserId!, now)
                    : new List<AttendanceLog>();

                var checkoutStates = new[]
                {
                    HRMConstants.CheckoutState,
                    HRMConstants.AutoCheckoutState,
                    HRMConstants.ForceCheckoutState,
                    HRMConstants.ExtraCheckOutState
                };

                // IMPORTANT:
                // A failed checkout attempt is also a checkout attempt and must
                // be considered before deciding whether additional work exists.
                // Successful checkout is determined from IsProcessed + Status.
                var latestCheckoutAttempt = await GetLatestCheckoutAttemptAsync(
                    employee.BiometricUserId,
                    now);

                if (latestCheckoutAttempt != null &&
                    !(latestCheckoutAttempt.IsProcessed &&
                      latestCheckoutAttempt.Status == "Success"))
                {
                    Log(
                        context,
                        ConsoleTextColor.Yellow,
                        "{0}: last checkout attempt at {1:yyyy-MM-dd HH:mm:ss} was not successful. Retrying checkout.",
                        employee.EmployeeName,
                        latestCheckoutAttempt.CheckTime);

                    return await ExecuteDirectCheckoutAsync(
                        employee,
                        now,
                        "Retry after unsuccessful previous checkout attempt",
                        context);
                }

                // Last successful checkout is the baseline for detecting
                // additional biometric work.
                var lastSuccessfulCheckout = logs
                    .Where(x =>
                        checkoutStates.Contains(x.AttendanceState) &&
                        x.IsProcessed &&
                        x.Status == "Success")
                    .OrderByDescending(x => x.CheckTime)
                    .FirstOrDefault();

                if (lastSuccessfulCheckout != null &&
                    !string.IsNullOrWhiteSpace(employee.BiometricUserId))
                {
                    var result = await ProcessExtraWorkingTimeAsync(
                        employee,
                        lastSuccessfulCheckout.CheckTime,
                        now,
                        context);

                    if (result.HasAdditionalWork)
                        return result;

                    if (!result.Success)
                        return result;

                    // No biometric work after the successful checkout. The
                    // employee still needs to be processed by this job.
                    return await ExecuteDirectCheckoutAsync(
                        employee,
                        now,
                        "No biometric punch after last successful checkout",
                        context);
                }

                // No previous successful checkout exists. Process the employee
                // rather than silently treating the employee as complete.
                return await ExecuteDirectCheckoutAsync(
                    employee,
                    now,
                    lastSuccessfulCheckout == null
                        ? "No previous successful checkout"
                        : "Employee has no biometric id for punch reconciliation",
                    context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Attendance processing failed for employee {EmployeeCode}",
                    employee.HRMEmployeeCode);

                Log(
                    context,
                    ConsoleTextColor.Red,
                    "Attendance processing exception for {0}: {1}",
                    employee.EmployeeName,
                    ex.Message);

                return ProcessResult.Failed(ex.Message);
            }
        }

        private async Task<ProcessResult> ExecuteDirectCheckoutAsync(
            EmployeeMapping employee,
            DateTime checkoutTime,
            string reason,
            PerformContext context)
        {
            var success = await SendAttendanceApiAsync(
                employee,
                HRMConstants.ForceCheckoutState,
                checkoutTime,
                reason,
                context);

            if (!success)
            {
                return ProcessResult.Failed(
                    $"Force checkout API failed. Reason: {reason}");
            }

            if (!string.IsNullOrWhiteSpace(employee.BiometricUserId))
            {
                await SaveAttendanceLogAsync(
                    employee.BiometricUserId!,
                    checkoutTime,
                    HRMConstants.ForceCheckoutState,
                    true,
                    $"Company force checkout successful. {reason}");
            }

            Log(
                context,
                ConsoleTextColor.Green,
                "Force checkout SUCCESS for {0} at {1:yyyy-MM-dd HH:mm:ss}. Reason: {2}",
                employee.EmployeeName,
                checkoutTime,
                reason);

            return ProcessResult.SuccessResult();
        }

        private async Task<AttendanceLog?> GetLatestCheckoutAttemptAsync(
            string? biometricUserId,
            DateTime now)
        {
            if (string.IsNullOrWhiteSpace(biometricUserId))
                return null;

            return await _dbContext.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.EmployeeCode == biometricUserId &&
                    x.CheckTime <= now &&
                    (x.AttendanceState == HRMConstants.CheckoutState ||
                     x.AttendanceState == HRMConstants.AutoCheckoutState ||
                     x.AttendanceState == HRMConstants.ForceCheckoutState ||
                     x.AttendanceState == HRMConstants.ExtraCheckOutState))
                .OrderByDescending(x => x.CheckTime)
                .ThenByDescending(x => x.ProcessedAt)
                .FirstOrDefaultAsync();
        }

        private async Task<ProcessResult> ProcessExtraWorkingTimeAsync(
            EmployeeMapping employee,
            DateTime lastCheckoutTime,
            DateTime now,
            PerformContext context)
        {
            var latestPunch = await _attendanceProvider.GetLatestPunchAfterAsync(
                employee.BiometricUserId!,
                lastCheckoutTime,
                now);

            if (!latestPunch.HasValue ||
                latestPunch.Value <= lastCheckoutTime)
            {
                Log(
                    context,
                    ConsoleTextColor.Gray,
                    "{0}: no biometric punch after last successful checkout {1:yyyy-MM-dd HH:mm:ss}.",
                    employee.EmployeeName,
                    lastCheckoutTime);

                return ProcessResult.NoAdditionalWorkResult();
            }

            var extraCheckoutTime = latestPunch.Value;
            var extraMinutes = (int)(extraCheckoutTime - lastCheckoutTime).TotalMinutes;

            if (extraMinutes <= 0)
                return ProcessResult.SuccessResult();

            // Reconciliation is idempotent. Do not send another check-in if
            // this exact extra session has already been created successfully.
            var hasExtraCheckIn = await HasAttendanceLogAsync(
                employee.BiometricUserId!,
                lastCheckoutTime,
                HRMConstants.ExtraCheckInState);

            if (!hasExtraCheckIn)
            {
                var checkInSuccess = await SendAttendanceApiAsync(
                    employee,
                    HRMConstants.CheckInState,
                    lastCheckoutTime,
                    $"Extra working-time check-in. Extra minutes: {extraMinutes}.",
                    context);

                if (!checkInSuccess)
                {
                    return ProcessResult.Failed(
                        "Extra working-time check-in API failed.");
                }

                await SaveAttendanceLogAsync(
                    employee.BiometricUserId!,
                    lastCheckoutTime,
                    HRMConstants.ExtraCheckInState,
                    true,
                    $"Company force checkout created extra-time check-in. Extra minutes: {extraMinutes}.");
            }

            var hasExtraCheckOut = await HasAttendanceLogAsync(
                employee.BiometricUserId!,
                extraCheckoutTime,
                HRMConstants.ExtraCheckOutState);

            if (!hasExtraCheckOut)
            {
                var checkOutSuccess = await SendAttendanceApiAsync(
                    employee,
                    HRMConstants.CheckoutState,
                    extraCheckoutTime,
                    $"Extra working-time checkout. Extra minutes: {extraMinutes}.",
                    context);

                if (!checkOutSuccess)
                {
                    return ProcessResult.Failed(
                        "Extra working-time checkout API failed.");
                }

                await SaveAttendanceLogAsync(
                    employee.BiometricUserId!,
                    extraCheckoutTime,
                    HRMConstants.ExtraCheckOutState,
                    true,
                    $"Company force checkout created extra-time checkout. Extra minutes: {extraMinutes}.");
            }

            Log(
                context,
                ConsoleTextColor.Green,
                "Extra working time SUCCESS for {0}: {1:dd-MM-yyyy HH:mm} - {2:dd-MM-yyyy HH:mm} ({3} minutes).",
                employee.EmployeeName,
                lastCheckoutTime,
                extraCheckoutTime,
                extraMinutes);

            return ProcessResult.AdditionalWorkSuccessResult();
        }

        private async Task ForceCheckoutFailedEmployeesAsync(
            IReadOnlyCollection<ForceCheckoutEmployee> failedEmployees,
            DateTime now,
            PerformContext context)
        {
            foreach (var failed in failedEmployees)
            {
                var employee = failed.Employee;

                // The current attendance API exposes the generic attendance
                // action endpoint. "Force checkout" is represented internally
                // by ForceCheckoutState; the API action remains "checkout".
                var success = await SendAttendanceApiAsync(
                    employee,
                    HRMConstants.ForceCheckoutState,
                    now,
                    $"Force checkout fallback. Previous processing failed: {failed.Reason}",
                    context);

                if (!success)
                {
                    Log(
                        context,
                        ConsoleTextColor.Red,
                        "Force checkout FAILED for {0}. Previous failure: {1}",
                        employee.EmployeeName,
                        failed.Reason);

                    continue;
                }

                if (!string.IsNullOrWhiteSpace(employee.BiometricUserId))
                {
                    await SaveAttendanceLogAsync(
                        employee.BiometricUserId!,
                        now,
                        HRMConstants.ForceCheckoutState,
                        true,
                        $"Company force checkout fallback. Previous failure: {failed.Reason}");
                }

                Log(
                    context,
                    ConsoleTextColor.Green,
                    "Force checkout SUCCESS for {0}. Previous failure: {1}",
                    employee.EmployeeName,
                    failed.Reason);
            }
        }

        private async Task<bool> SendAttendanceApiAsync(
            EmployeeMapping employee,
            string state,
            DateTime attendanceTime,
            string reason,
            PerformContext context)
        {
            // The external endpoint accepts the normal attendance action
            // values (checkin/checkout). ForceCheckoutState is an internal
            // audit state, so the API request uses "checkout".
            var apiType = state == HRMConstants.CheckInState
                ? HRMConstants.CheckInState
                : HRMConstants.CheckoutState;

            var request = new AttendanceAPIDto
            {
                employee_code = employee.HRMEmployeeCode,
                type = apiType,
                date_time = attendanceTime
            };

            var success = await _apiService.SendAsync(request);

            var status = success ? "SUCCESS" : "FAILED";

            _logger.LogInformation(
                "JobCompanyForceCheckout API result | Employee={EmployeeCode} | EmployeeName={EmployeeName} | ApiAction={ApiAction} | State={State} | Time={AttendanceTime:yyyy-MM-dd HH:mm:ss} | Status={Status} | Reason={Reason}",
                employee.HRMEmployeeCode,
                employee.EmployeeName,
                apiType,
                state,
                attendanceTime,
                status,
                reason);

            context.WriteLine(
                success ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                "API {0} | Employee={1} | Action={2} | State={3} | Time={4:yyyy-MM-dd HH:mm:ss} | Reason={5}",
                status,
                employee.EmployeeName,
                apiType,
                state,
                attendanceTime,
                reason);

            return success;
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
                    (x.AttendanceState == HRMConstants.CheckInState ||
                     x.AttendanceState == HRMConstants.CheckoutState ||
                     x.AttendanceState == HRMConstants.AutoCheckoutState ||
                     x.AttendanceState == HRMConstants.ForceCheckoutState ||
                     x.AttendanceState == HRMConstants.ExtraCheckInState ||
                     x.AttendanceState == HRMConstants.ExtraCheckOutState))
                .OrderByDescending(x => x.CheckTime)
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

        private void Log(
            PerformContext context,
            ConsoleTextColor color,
            string message,
            params object[] args)
        {
            var formattedMessage = string.Format(message, args);

            _logger.LogInformation(
                "JobCompanyForceCheckout | {Message}",
                formattedMessage);

            context.WriteLine(
                color,
                formattedMessage);
        }

        private sealed class ForceCheckoutEmployee
        {
            public required EmployeeMapping Employee { get; init; }
            public required string Reason { get; init; }
        }

        private sealed class ProcessResult
        {
            public bool Success { get; init; }
            public string? FailureReason { get; init; }
            public bool HasAdditionalWork { get; init; }

            public static ProcessResult SuccessResult()
                => new() { Success = true };

            public static ProcessResult NoAdditionalWorkResult()
                => new() { Success = true, HasAdditionalWork = false };

            public static ProcessResult AdditionalWorkSuccessResult()
                => new() { Success = true, HasAdditionalWork = true };

            public static ProcessResult Failed(string reason)
                => new()
                {
                    Success = false,
                    FailureReason = reason
                };
        }
    }
}
