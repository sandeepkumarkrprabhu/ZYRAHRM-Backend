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
    public class JobAutoCheckoutJob : IAutoCheckoutJob
    {
        private readonly IEmployeeService _employeeService;
        private readonly IAttendanceProvider _attendanceProvider;
        private readonly IAttendanceApiService _apiService;
        private readonly IJobService _jobService;
        private readonly AttendanceDbContext _dbContext;
        private readonly IExtraTimeEvaluationService _extraTimeEvaluationService;
        private readonly ILogger<JobAutoCheckoutJob> _logger;

        public JobAutoCheckoutJob(
            IEmployeeService employeeService,
            IAttendanceProvider attendanceProvider,
            IAttendanceApiService apiService,
            IJobService jobService,
            AttendanceDbContext dbContext,
            IExtraTimeEvaluationService extraTimeEvaluationService,
            ILogger<JobAutoCheckoutJob> logger)
        {
            _employeeService = employeeService;
            _attendanceProvider = attendanceProvider;
            _apiService = apiService;
            _jobService = jobService;
            _dbContext = dbContext;
            _extraTimeEvaluationService = extraTimeEvaluationService;
            _logger = logger;
        }

        public async Task Execute(PerformContext context)
        {
            try
            {
                var now = DateTime.Now;
                var today = now.Date;
                var oneHourAgo = now.AddHours(-1);

                _logger.LogInformation(
                    "AutoCheckoutJob started at {Time}", now);

                context.WriteLine(
                    ConsoleTextColor.Cyan,
                    $"Auto checkout job started at {now:yyyy-MM-dd HH:mm:ss}");

                // ============================================================
                // 1. GET ALL ACTIVE AUTO CHECKOUT POLICIES
                // ============================================================

                var policyRules = await (
                    from master in _dbContext.AttendancePolicyMasters

                    join autoCheckoutRule in _dbContext.AttendancePolicyRules
                        on master.Id equals autoCheckoutRule.AttendancePolicyId

                    join shiftStartRule in _dbContext.AttendancePolicyRules
                        .Where(x => x.RuleCode == HRMConstants.SHIFT_START_TIME_NAME)
                        on master.Id equals shiftStartRule.AttendancePolicyId
                        into shiftStartRules

                    from shiftStartRule in shiftStartRules.DefaultIfEmpty()

                    where master.IsEnable
                          && autoCheckoutRule.RuleCode ==
                             HRMConstants.SHIFT_AUTO_CHECKOUT_NAME
                          && autoCheckoutRule.RuleValue != null
                          && shiftStartRule != null
                          && shiftStartRule.RuleValue != null

                    select new
                    {
                        Policy = master,
                        AutoCheckoutTime = autoCheckoutRule.RuleValue,
                        ShiftStartTime = shiftStartRule.RuleValue
                    }
                ).ToListAsync();


                if (!policyRules.Any())
                {
                    context.WriteLine(
                        ConsoleTextColor.Gray,
                        "No auto checkout policies configured.");

                    return;
                }


                // ============================================================
                // 2. FIND POLICIES WHOSE AUTO CHECKOUT TIME IS DUE
                // ============================================================

                var eligiblePolicies = policyRules
                    .Select(policy =>
                    {
                        if (!TimeSpan.TryParse(
                                policy.AutoCheckoutTime,
                                out var autoCheckoutTime))
                        {
                            return null;
                        }

                        var scheduledTime = today.Add(autoCheckoutTime);

                        if (scheduledTime > now ||
                            scheduledTime < oneHourAgo)
                        {
                            return null;
                        }

                        if (!TimeSpan.TryParse(
                                policy.ShiftStartTime,
                                out var shiftStartTime))
                        {
                            return null;
                        }

                        return new
                        {
                            Policy = policy.Policy,
                            ScheduledTime = scheduledTime,
                            ShiftStartTime = today.Add(shiftStartTime)
                        };
                    })
                    .Where(x => x != null)
                    .ToList();


                if (!eligiblePolicies.Any())
                {
                    context.WriteLine(
                        ConsoleTextColor.Gray,
                        "No policies are currently eligible for auto checkout.");

                    return;
                }


                context.WriteLine(
                    ConsoleTextColor.Cyan,
                    $"Found {eligiblePolicies.Count} eligible policies.");


                // ============================================================
                // 3. PROCESS EACH ELIGIBLE POLICY
                // ============================================================

                foreach (var policyRec in eligiblePolicies)
                {
                    var policy = policyRec.Policy;
                    var policyAutoCheckoutTime = policyRec.ScheduledTime;

                    context.WriteLine(
                        ConsoleTextColor.Yellow,
                        $"Policy: {policy.PolicyName}, " +
                        $"Auto checkout: " +
                        $"{policyAutoCheckoutTime:yyyy-MM-dd HH:mm}");

                    var employees = await _employeeService.GetEmployeeByAttendancePolicy(policy.Id);

                    if (!employees.Any())
                    {
                        context.WriteLine(
                            ConsoleTextColor.Gray,
                            $"No employees assigned to {policy.PolicyName}");

                        continue;
                    }

                    context.WriteLine(
                        ConsoleTextColor.Cyan,
                        $"Found {employees.Count} employees " +
                        $"for {policy.PolicyName}");

                    foreach (var employee in employees)
                    {
                        await ProcessEmployeeAutoCheckoutAsync(
                            employee,
                            policy,
                            policyAutoCheckoutTime,
                            policyRec.ShiftStartTime,
                            today,
                            context, now);
                    }
                }


                // ============================================================
                // 5. JOB COMPLETED
                // ============================================================

                _logger.LogInformation(
                    "AutoCheckoutJob completed successfully.");

                context.WriteLine(
                    ConsoleTextColor.Green,
                    "Auto checkout completed.");
            }
            catch (Exception ex)
            {
                // ------------------------------------------------------------
                // IMPORTANT:
                // Do not swallow the exception.
                // Hangfire needs the exception to mark the job as failed
                // and perform its retry mechanism if configured.
                // ------------------------------------------------------------

                _logger.LogError(
                    ex,
                    "AutoCheckoutJob failed.");

                context.WriteLine(
                    ConsoleTextColor.Red,
                    $"Auto checkout job failed: {ex.Message}");

                throw;
            }
        }

        private async Task ProcessEmployeeAutoCheckoutAsync(
            EmployeeMapping employee,
            AttendancePolicyMaster policy,
            DateTime policyAutoCheckoutTime,
            DateTime shiftStartTime,
            DateTime today,
            PerformContext context,
            DateTime executionTime)
        {
            if (string.IsNullOrWhiteSpace(employee.HRMEmployeeCode) ||
                string.IsNullOrWhiteSpace(employee.BiometricUserId))
            {
                var reason = "Skipped: HRM employee code or biometric user ID is missing.";

                _logger.LogWarning(
                    "AutoCheckout | Employee={EmployeeName} | CheckIn=UNKNOWN | Checkout=NOT_CREATED | Status=Skipped | Reason={Reason}",
                    employee.EmployeeName,
                    reason);

                context.WriteLine(
                    ConsoleTextColor.Yellow,
                    $"{employee.EmployeeName}: {reason}");

                return;
            }

            // AUTO_CHECKOUT_TIME determines when this reconciliation job runs.
            // It is not used as a fallback checkout timestamp. Company Force
            // Checkout remains a separate job and policy.

            var attendanceLogs = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.EmployeeCode == employee.BiometricUserId &&
                    x.CheckTime >= today.AddDays(-1) &&
                    x.CheckTime <= executionTime &&
                    x.IsProcessed &&
                    x.Status == "Success")
                .OrderByDescending(x => x.CheckTime)
                .ToListAsync();

            var latestCheckIn = attendanceLogs
                .Where(x =>
                    x.AttendanceState == HRMConstants.CheckInState ||
                    x.AttendanceState == HRMConstants.ManualCheckInState)
                .OrderByDescending(x => x.CheckTime)
                .FirstOrDefault();

            // A manual check-in is a valid open session too. A completed
            // checkout after that check-in means the session is already closed.
            var checkoutSearchFrom = latestCheckIn?.CheckTime ?? shiftStartTime;

            var latestCheckout = attendanceLogs
                .Where(x =>
                    (x.AttendanceState == HRMConstants.CheckoutState ||
                     x.AttendanceState == HRMConstants.AutoCheckoutState ||
                     x.AttendanceState == HRMConstants.ForceCheckoutState) &&
                    x.CheckTime > checkoutSearchFrom)
                .OrderByDescending(x => x.CheckTime)
                .FirstOrDefault();

            _logger.LogInformation(
                "AutoCheckout | Employee={EmployeeName} | BiometricUserId={BiometricUserId} | CheckIn={CheckIn} | ExistingCheckout={ExistingCheckout} | PolicyCheckoutTrigger={PolicyCheckoutTrigger} | ExecutionTime={ExecutionTime}",
                employee.EmployeeName,
                employee.BiometricUserId,
                latestCheckIn?.CheckTime,
                latestCheckout?.CheckTime,
                policyAutoCheckoutTime,
                executionTime);

            if (latestCheckout != null)
            {
                var extraPunch = await _attendanceProvider.GetLatestPunchAfterAsync(
                    employee.BiometricUserId,
                    latestCheckout.CheckTime,
                    executionTime);

                if (extraPunch.HasValue && extraPunch.Value > latestCheckout.CheckTime)
                {
                    await ProcessExtraWorkingTimeAsync(
                        employee,
                        latestCheckout.CheckTime,
                        extraPunch.Value,
                        context);

                    return;
                }

                var checkInText = latestCheckIn?.CheckTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "UNKNOWN";
                var checkoutText = latestCheckout.CheckTime.ToString("yyyy-MM-dd HH:mm:ss");
                var message =
                    $"{employee.EmployeeName} | CheckIn={checkInText} | " +
                    $"Checkout={checkoutText} | Status=Skipped | " +
                    "Reason=Already checked out; no later biometric punch found.";

                _logger.LogInformation(
                    "AutoCheckout | Employee={EmployeeName} | CheckIn={CheckIn} | Checkout={Checkout} | Status=Skipped | Reason=Already checked out; no later biometric punch found.",
                    employee.EmployeeName,
                    latestCheckIn?.CheckTime,
                    latestCheckout.CheckTime);

                context.WriteLine(ConsoleTextColor.Gray, message);
                return;
            }

            if (latestCheckIn == null)
            {
                const string reason = "No open attendance session found.";
                _logger.LogInformation(
                    "AutoCheckout | Employee={EmployeeName} | CheckIn=UNKNOWN | Checkout=NOT_CREATED | Status=Skipped | Reason={Reason}",
                    employee.EmployeeName,
                    reason);

                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"{employee.EmployeeName} | CheckIn=UNKNOWN | Checkout=NOT_CREATED | Status=Skipped | Reason={reason}");

                return;
            }

            // The latest punch is searched after the open check-in, so this
            // works for both manual and biometric check-ins.
            var latestPunch = await _attendanceProvider.GetLatestPunchAfterAsync(
                employee.BiometricUserId,
                latestCheckIn.CheckTime,
                executionTime);

            if (!latestPunch.HasValue || latestPunch.Value <= latestCheckIn.CheckTime)
            {
                const string reason = "No valid biometric punch exists after check-in; checkout was skipped.";
                _logger.LogInformation(
                    "AutoCheckout | Employee={EmployeeName} | CheckIn={CheckIn} | Checkout=NOT_CREATED | Status=Skipped | Reason={Reason}",
                    employee.EmployeeName,
                    latestCheckIn.CheckTime,
                    reason);

                context.WriteLine(
                    ConsoleTextColor.Yellow,
                    $"{employee.EmployeeName} | CheckIn={latestCheckIn.CheckTime:yyyy-MM-dd HH:mm:ss} | " +
                    $"Checkout=NOT_CREATED | Status=Skipped | Reason={reason}");

                return;
            }

            var checkoutTime = latestPunch.Value;
            var apiSuccess = await _apiService.SendAsync(
                new AttendanceAPIDto
                {
                    employee_code = employee.HRMEmployeeCode,
                    type = HRMConstants.CheckoutState,
                    date_time = checkoutTime
                });

            // Record successful and failed attempts so the job has an audit
            // trail. Failed attempts are not treated as completed checkouts.
            _dbContext.AttendanceLogs.Add(new AttendanceLog
            {
                EmployeeCode = employee.BiometricUserId,
                CheckTime = checkoutTime,
                AttendanceState = HRMConstants.AutoCheckoutState,
                IsProcessed = apiSuccess,
                ProcessedAt = DateTime.Now,
                Status = apiSuccess ? "Success" : "Failed",
                ErrorMessage = apiSuccess
                    ? "Auto Checkout used the latest biometric punch."
                    : "Auto Checkout API failed while applying the latest biometric punch."
            });

            await _dbContext.SaveChangesAsync();

            var resultStatus = apiSuccess ? "Checkout Created" : "Checkout Failed";
            var resultMessage =
                $"{employee.EmployeeName} | CheckIn={latestCheckIn.CheckTime:yyyy-MM-dd HH:mm:ss} | " +
                $"Checkout={checkoutTime:yyyy-MM-dd HH:mm:ss} | " +
                $"Source=Latest biometric punch | Status={resultStatus}";

            _logger.LogInformation(
                "AutoCheckout | Employee={EmployeeName} | CheckIn={CheckIn} | Checkout={Checkout} | Source=Latest biometric punch | Status={Status}",
                employee.EmployeeName,
                latestCheckIn.CheckTime,
                checkoutTime,
                resultStatus);

            context.WriteLine(
                apiSuccess ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                resultMessage);
        }

        private async Task ProcessExtraWorkingTimeAsync(
            EmployeeMapping employee,
            DateTime previousCheckoutTime,
            DateTime extraPunchTime,
            PerformContext context)
        {
            // When the employee has already checked out and then punches again,
            // the later punch represents the end of the extra working session.
            //
            // Example:
            //   18:30 normal checkout
            //   20:12 biometric punch
            //
            // The extra session is therefore:
            //   18:30 extra check-in -> 20:12 extra checkout
            //
            // Do NOT use AUTO_CHECKOUT_TIME as the extra checkout here.
            // AUTO_CHECKOUT_TIME only determines when this reconciliation
            // job runs; the biometric punch is the actual attendance time.

            if (extraPunchTime <= previousCheckoutTime)
                return;

            var hasExtraCheckIn = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == employee.BiometricUserId &&
                    x.CheckTime == previousCheckoutTime &&
                    x.AttendanceState == HRMConstants.ExtraCheckInState &&
                    x.IsProcessed &&
                    x.Status == "Success");

            if (!hasExtraCheckIn)
            {
                var checkInSuccess = await _apiService.SendAsync(
                    new AttendanceAPIDto
                    {
                        employee_code = employee.HRMEmployeeCode,
                        type = HRMConstants.CheckInState,
                        date_time = previousCheckoutTime
                    });

                if (!checkInSuccess)
                {
                    context.WriteLine(
                        ConsoleTextColor.Red,
                        $"Extra-time check-in FAILED for {employee.EmployeeName} at " +
                        $"{previousCheckoutTime:yyyy-MM-dd HH:mm:ss}");

                    return;
                }

                _dbContext.AttendanceLogs.Add(new AttendanceLog
                {
                    EmployeeCode = employee.BiometricUserId,
                    CheckTime = previousCheckoutTime,
                    AttendanceState = HRMConstants.ExtraCheckInState,
                    IsProcessed = true,
                    Status = "Success",
                    ErrorMessage =
                        "Extra working session started at the previous normal checkout time."
                });

                await _dbContext.SaveChangesAsync();
            }

            var hasExtraCheckOut = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == employee.BiometricUserId &&
                    x.CheckTime == extraPunchTime &&
                    x.AttendanceState == HRMConstants.ExtraCheckOutState &&
                    x.IsProcessed &&
                    x.Status == "Success");

            if (hasExtraCheckOut)
            {
                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"Extra-time checkout already processed for {employee.EmployeeName} at " +
                    $"{extraPunchTime:yyyy-MM-dd HH:mm:ss}");

                return;
            }

            var checkOutSuccess = await _apiService.SendAsync(
                new AttendanceAPIDto
                {
                    employee_code = employee.HRMEmployeeCode,
                    type = HRMConstants.CheckoutState,
                    date_time = extraPunchTime
                });

            if (!checkOutSuccess)
            {
                context.WriteLine(
                    ConsoleTextColor.Red,
                    $"Extra-time checkout FAILED for {employee.EmployeeName} at " +
                    $"{extraPunchTime:yyyy-MM-dd HH:mm:ss}");

                return;
            }

            var extraMinutes = (int)(extraPunchTime - previousCheckoutTime).TotalMinutes;

            _dbContext.AttendanceLogs.Add(new AttendanceLog
            {
                EmployeeCode = employee.BiometricUserId,
                CheckTime = extraPunchTime,
                AttendanceState = HRMConstants.ExtraCheckOutState,
                IsProcessed = true,
                Status = "Success",
                ErrorMessage =
                    $"Extra working-time checkout. Extra minutes: {extraMinutes}."
            });

            await _dbContext.SaveChangesAsync();

            context.WriteLine(
                ConsoleTextColor.Green,
                $"Extra working time SUCCESS for {employee.EmployeeName}: " +
                $"{previousCheckoutTime:yyyy-MM-dd HH:mm:ss} - " +
                $"{extraPunchTime:yyyy-MM-dd HH:mm:ss} ({extraMinutes} minutes).");
        }

        private void LogInformation(PerformContext? context, string message, ConsoleTextColor? color = null)
        {
            _logger.LogInformation(message);

            context?.WriteLine(
                color ?? ConsoleTextColor.Yellow,
                message);
        }
    }
}