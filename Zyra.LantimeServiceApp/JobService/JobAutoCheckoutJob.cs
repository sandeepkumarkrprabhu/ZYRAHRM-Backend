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
            // Do not use shift start/end as the eligibility condition.
            // Employees may check in before their scheduled shift.
            //
            // The policy auto-checkout time is the eligibility boundary.
            // Only attendance that is still open at that point is considered.

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
                     x.AttendanceState == "Auto checkout") &&
                    x.CheckTime > checkoutSearchFrom)
                .OrderByDescending(x => x.CheckTime)
                .FirstOrDefault();

            // AttendanceLogs check-in is optional because attendance may
            // also be marked manually. If a check-in exists, use it as the
            // lower boundary for biometric punches. Otherwise, use the shift
            // start time as the lower boundary.
            if (latestCheckIn != null &&
                latestCheckout != null &&
                latestCheckout.CheckTime >= latestCheckIn.CheckTime)
            {
                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"{employee.EmployeeName} already checked out at {latestCheckout.CheckTime:yyyy-MM-dd HH:mm:ss}");

                return;
            }

            // A check-in after the policy time cannot be checked out using an
            // earlier policy timestamp. However, when there is no AttendanceLogs
            // check-in, manual attendance may still require biometric reconciliation.
            if (latestCheckIn != null &&
                latestCheckIn.CheckTime >= policyAutoCheckoutTime)
            {
                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"Auto checkout skipped for {employee.EmployeeName}: " +
                    $"check-in {latestCheckIn.CheckTime:yyyy-MM-dd HH:mm:ss} " +
                    $"is after policy checkout time {policyAutoCheckoutTime:yyyy-MM-dd HH:mm:ss}.");

                return;
            }

            // At the moment auto checkout runs, take the latest biometric
            // punch after the applicable session boundary:
            // - AttendanceLogs check-in, when available.
            // - Otherwise the configured shift start time.
            // This supports both normal and manually-created attendance.
            var autoCheckoutNow = executionTime;
            var biometricSearchFrom = latestCheckIn?.CheckTime ?? shiftStartTime;

            var latestPunch = await _attendanceProvider.GetLatestPunchAfterAsync(
                employee.BiometricUserId,
                biometricSearchFrom,
                autoCheckoutNow);

            _logger.LogInformation(
                "AutoCheckout decision for {EmployeeName}: BiometricUserId={BiometricUserId}, " +
                "AttendanceLogCheckIn={CheckIn}, AttendanceLogCheckout={Checkout}, " +
                "LatestBiometricPunch={LatestPunch}, PolicyCheckout={PolicyCheckout}, Now={Now}",
                employee.EmployeeName,
                employee.BiometricUserId,
                latestCheckIn?.CheckTime,
                latestCheckout?.CheckTime,
                latestPunch,
                policyAutoCheckoutTime,
                autoCheckoutNow);

            context.WriteLine(
                ConsoleTextColor.Cyan,
                $"{employee.EmployeeName}: CheckIn={latestCheckIn?.CheckTime:yyyy-MM-dd HH:mm:ss}, " +
                $"PunchSearchFrom={biometricSearchFrom:yyyy-MM-dd HH:mm:ss}, " +
                $"ExistingCheckout={latestCheckout?.CheckTime:yyyy-MM-dd HH:mm:ss}, " +
                $"LatestBiometricPunch={latestPunch?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NONE"}, " +
                $"PolicyCheckout={policyAutoCheckoutTime:yyyy-MM-dd HH:mm:ss}");

            var isValidBiometricCheckout =
                latestPunch.HasValue &&
                latestPunch.Value > biometricSearchFrom;

            if (isValidBiometricCheckout)
            {
                var biometricCheckoutTime = latestPunch!.Value;

                var result = await _apiService.SendAsync(
                    new AttendanceAPIDto
                    {
                        employee_code = employee.HRMEmployeeCode,
                        type = "checkout",
                        date_time = biometricCheckoutTime
                    });

                if (result)
                {
                    _dbContext.AttendanceLogs.Add(new AttendanceLog
                    {
                        EmployeeCode = employee.BiometricUserId,
                        CheckTime = biometricCheckoutTime,
                        AttendanceState = "checkout",
                        IsProcessed = true,
                        Status = "Success",
                        ErrorMessage = "Biometric punch used as checkout by auto checkout job"
                    });

                    await _dbContext.SaveChangesAsync();
                }

                context.WriteLine(
                    result ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                    $"Biometric checkout {(result ? "SUCCESS" : "FAILED")} " +
                    $"for {employee.EmployeeName} at {biometricCheckoutTime:yyyy-MM-dd HH:mm:ss}");

                return;
            }

            // No usable biometric punch exists after the check-in.
            // Therefore, use the attendance policy's auto-checkout time.
            _logger.LogInformation(
                "AutoCheckout fallback for {EmployeeName}: no valid biometric punch after " +
                "{PunchSearchFrom}. LatestBiometricPunch={LatestPunch}, CheckIn={CheckIn}, " +
                "PolicyCheckout={PolicyCheckout}",
                employee.EmployeeName,
                biometricSearchFrom,
                latestPunch,
                latestCheckIn?.CheckTime,
                policyAutoCheckoutTime);
            var autoCheckoutResult = await _apiService.SendAsync(
                new AttendanceAPIDto
                {
                    employee_code = employee.HRMEmployeeCode,
                    type = "checkout",
                    date_time = policyAutoCheckoutTime
                });

            if (autoCheckoutResult)
            {
                _dbContext.AttendanceLogs.Add(new AttendanceLog
                {
                    EmployeeCode = employee.BiometricUserId,
                    CheckTime = policyAutoCheckoutTime,
                    AttendanceState = "Auto checkout",
                    IsProcessed = true,
                    Status = "Success",
                    ErrorMessage = "Policy auto checkout"
                });

                await _dbContext.SaveChangesAsync();
            }

            context.WriteLine(
                autoCheckoutResult ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                $"Auto checkout {(autoCheckoutResult ? "SUCCESS" : "FAILED")} " +
                $"for {employee.EmployeeName} using policy {policy.PolicyName} " +
                $"at {policyAutoCheckoutTime:yyyy-MM-dd HH:mm:ss}");
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