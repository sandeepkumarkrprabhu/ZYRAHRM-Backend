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
                return;

            // IMPORTANT:
            // AUTO_CHECKOUT_TIME is the time this reconciliation job runs.
            // It is NOT an employee checkout timestamp.
            //
            // The job uses that configured time only to decide when the
            // reconciliation process is due. Actual attendance times come
            // from the biometric device or from already synchronized logs.

            // Get the latest successful check-in/check-out records for the
            // employee. Include the previous calendar day so an overnight
            // attendance session can remain open after midnight.
            var attendanceLogs = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.EmployeeCode == employee.BiometricUserId &&
                    x.CheckTime >= today.AddDays(-1) &&
                    x.CheckTime <= DateTime.Now &&
                    x.IsProcessed &&
                    x.Status == "Success")
                .OrderByDescending(x => x.CheckTime)
                .ToListAsync();

            _logger.LogInformation(
                "Processing auto checkout for {EmployeeName}: BiometricUserId={BiometricUserId}, " +
                "AttendanceLogsCount={AttendanceLogsCount}, PolicyCheckout={PolicyCheckout}, ShiftStart={ShiftStart}",
                employee.EmployeeName,
                employee.BiometricUserId,
                attendanceLogs.Count,
                policyAutoCheckoutTime,
                shiftStartTime);

            var latestCheckIn = attendanceLogs
                .Where(x => x.AttendanceState == "checkin")
                .OrderByDescending(x => x.CheckTime)
                .FirstOrDefault();

            // The checkout session boundary is the AttendanceLogs check-in
            // when available. Otherwise use the shift start time. This prevents
            // a previous day's checkout from being treated as today's checkout.
            var checkoutSearchFrom = latestCheckIn?.CheckTime ?? shiftStartTime;

            var latestCheckout = attendanceLogs
                .Where(x =>
                    (x.AttendanceState == "checkout" ||
                     x.AttendanceState == "Auto checkout" ||
                     x.AttendanceState == HRMConstants.ForceCheckoutState ||
                     x.AttendanceState == HRMConstants.ExtraCheckOutState) &&
                    x.CheckTime > checkoutSearchFrom)
                .OrderByDescending(x => x.CheckTime)
                .FirstOrDefault();

            // A successful checkout closes the normal attendance session.
            // However, a later biometric punch means the employee started
            // working again. Reconcile that later work as a separate session
            // instead of returning early.
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

                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"{employee.EmployeeName} already checked out at " +
                    $"{latestCheckout.CheckTime:yyyy-MM-dd HH:mm:ss}; no later punch found.");

                return;
            }

            // No successful checkout exists for this attendance session.
            // Use the successful check-in time when available, otherwise the
            // scheduled shift start. Only a real biometric punch may be used
            // as the checkout timestamp; never use the job execution time.
            var biometricSearchFrom = latestCheckIn?.CheckTime ?? shiftStartTime;

            var latestPunch = await _attendanceProvider.GetLatestPunchAfterAsync(
                employee.BiometricUserId,
                biometricSearchFrom,
                executionTime);

            _logger.LogInformation(
                "AutoCheckout reconciliation for {EmployeeName}: BiometricUserId={BiometricUserId}, " +
                "CheckIn={CheckIn}, ExistingCheckout={Checkout}, ReferenceTime={ReferenceTime}, " +
                "LatestBiometricPunch={LatestPunch}, JobExecutionTime={ExecutionTime}",
                employee.EmployeeName,
                employee.BiometricUserId,
                latestCheckIn?.CheckTime,
                latestCheckout?.CheckTime,
                biometricSearchFrom,
                latestPunch,
                executionTime);

            context.WriteLine(
                ConsoleTextColor.Cyan,
                $"{employee.EmployeeName}: CheckIn={latestCheckIn?.CheckTime:yyyy-MM-dd HH:mm:ss}, " +
                $"ExistingCheckout={latestCheckout?.CheckTime:yyyy-MM-dd HH:mm:ss}, " +
                $"ReferenceTime={biometricSearchFrom:yyyy-MM-dd HH:mm:ss}, " +
                $"LatestBiometricPunch={latestPunch?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NONE"}, " +
                $"JobExecutionTime={executionTime:yyyy-MM-dd HH:mm:ss}");

            if (latestPunch.HasValue && latestPunch.Value > biometricSearchFrom)
            {
                var checkoutSuccess = await _apiService.SendAsync(
                    new AttendanceAPIDto
                    {
                        employee_code = employee.HRMEmployeeCode,
                        type = HRMConstants.CheckoutState,
                        date_time = latestPunch.Value
                    });

                if (!checkoutSuccess)
                {
                    _logger.LogWarning(
                        "Biometric checkout API failed for {EmployeeName} at {CheckoutTime}.",
                        employee.EmployeeName,
                        latestPunch.Value);

                    context.WriteLine(
                        ConsoleTextColor.Red,
                        $"{employee.EmployeeName}: checkout API failed for biometric punch " +
                        $"{latestPunch.Value:yyyy-MM-dd HH:mm:ss}. Company Force Checkout remains the fallback.");

                    return;
                }

                _dbContext.AttendanceLogs.Add(new AttendanceLog
                {
                    EmployeeCode = employee.BiometricUserId,
                    CheckTime = latestPunch.Value,
                    AttendanceState = HRMConstants.CheckoutState,
                    IsProcessed = true,
                    ProcessedAt = DateTime.Now,
                    Status = "Success",
                    ErrorMessage = latestCheckIn == null
                        ? "Checkout created from biometric punch after scheduled shift start; no successful check-in log was found."
                        : "Open attendance session closed using the latest valid biometric punch."
                });

                await _dbContext.SaveChangesAsync();

                context.WriteLine(
                    ConsoleTextColor.Green,
                    $"{employee.EmployeeName}: attendance session closed at biometric punch " +
                    $"{latestPunch.Value:yyyy-MM-dd HH:mm:ss}.");

                return;
            }

            context.WriteLine(
                ConsoleTextColor.Gray,
                $"{employee.EmployeeName}: no valid biometric punch after reference time " +
                $"{biometricSearchFrom:yyyy-MM-dd HH:mm:ss}. Existing force-checkout rules remain the fallback.");
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