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

        #region Commented Out Old Logic
        //public async Task Execute(PerformContext context)
        //{
        //    try
        //    {
        //        var lastSync = _jobService.GetLastSyncTime();

        //        _logger.LogInformation("AutoCheckoutJob started at {Time}", DateTime.Now);
        //        context.WriteLine(ConsoleTextColor.Cyan, "Auto checkout job started...");

        //        // -----------------------------
        //        // STEP 1: FETCH DATA
        //        // -----------------------------
        //        //Attendance from Lantime Biometric Device
        //        var biometricRecords = await _attendanceProvider.GetAttendanceAsync(lastSync);
        //        var employees = await _employeeService.GetActiveEmployees();

        //        var zyraAttendanceLogs = await _dbContext.AttendanceLogs
        //            .Where(f => f.CheckTime >= lastSync &&
        //                        f.IsProcessed == true)
        //            .ToListAsync();

        //        if (employees == null || !employees.Any())
        //        {
        //            _logger.LogWarning("No employees found for auto checkout");
        //            return;
        //        }

        //        // -----------------------------
        //        // STEP 2: BUILD LOOKUPS
        //        // -----------------------------
        //        var biometricAttendanceRecLookup = biometricRecords
        //            .GroupBy(r => r.EmployeeCode)
        //            .ToDictionary(
        //                g => g.Key,
        //                g => g.OrderByDescending(x => x.CheckOutTime).FirstOrDefault()
        //            );

        //        var attendanceGrouped = zyraAttendanceLogs
        //            .GroupBy(a => a.EmployeeCode)
        //            .ToDictionary(g => g.Key, g => g.ToList());

        //        var checkoutLookup = zyraAttendanceLogs
        //            .Where(a => a.AttendanceState == "checkout")
        //            .GroupBy(a => a.EmployeeCode)
        //            .ToDictionary(
        //                g => g.Key,
        //                g => g.OrderByDescending(x => x.CheckTime).FirstOrDefault()
        //            );

        //        var employeesToSkip = new HashSet<string>();
        //        var newAutoCheckInOutEmps = new List<EmployeeMapping>();

        //        // -----------------------------
        //        // STEP 3: DETECT MISMATCH
        //        // -----------------------------
        //        foreach (var employee in employees)
        //        {
        //            if (string.IsNullOrEmpty(employee.BiometricUserId))
        //                continue;

        //            //Biometric Device last Puch time
        //            biometricAttendanceRecLookup.TryGetValue(employee.BiometricUserId, out var lastPunch);

        //            //Attendance from ZYRA Attendance Logs
        //            checkoutLookup.TryGetValue(employee.BiometricUserId, out var lastCheckout);

        //            if (lastPunch == null)
        //                continue;

        //            var isDiffStatus =
        //                lastCheckout == null ||
        //                Math.Abs((lastPunch.CheckOutTime - lastCheckout.CheckTime).TotalMinutes) > 10;

        //            if (isDiffStatus)
        //            {
        //                newAutoCheckInOutEmps.Add(employee);
        //                employeesToSkip.Add(employee.BiometricUserId);

        //                context.WriteLine(ConsoleTextColor.Yellow,
        //                    $"Mismatch: {employee.EmployeeName}, checkout time : {lastPunch.CheckOutTime}");
        //            }
        //        }

        //        // -----------------------------
        //        // STEP 4: FIX ATTENDANCE (NEW LOGIC)
        //        // -----------------------------
        //        foreach (var emp in newAutoCheckInOutEmps)
        //        {
        //            if (string.IsNullOrEmpty(emp.HRMEmployeeCode) ||
        //                string.IsNullOrEmpty(emp.BiometricUserId))
        //                continue;

        //            biometricAttendanceRecLookup.TryGetValue(emp.BiometricUserId, out var lastPunch);
        //            checkoutLookup.TryGetValue(emp.BiometricUserId, out var existingCheckout);

        //            if (lastPunch == null || existingCheckout == null)
        //                continue;

        //            var checkInTime = existingCheckout.CheckTime;
        //            var checkOutTime = lastPunch.CheckOutTime;

        //            if (checkOutTime <= checkInTime)
        //                continue;

        //            attendanceGrouped.TryGetValue(emp.BiometricUserId, out var empLogs);

        //            // CHECK-IN
        //            var hasCheckIn = empLogs?.Any(a =>
        //                a.AttendanceState == "checkin" &&
        //                Math.Abs((a.CheckTime - checkInTime).TotalMinutes) <= 1) ?? false;

        //            if (!hasCheckIn)
        //            {
        //                await _apiService.SendAsync(new AttendanceAPIDto
        //                {
        //                    employee_code = emp.HRMEmployeeCode,
        //                    type = "checkin",
        //                    date_time = checkInTime
        //                });

        //                context.WriteLine(ConsoleTextColor.Green,
        //                    $"Check-in FIXED Extra for {emp.EmployeeName}, check in time {checkInTime}");
        //            }

        //            // CHECK-OUT
        //            var hasCheckOut = empLogs?.Any(a =>
        //                a.AttendanceState == "checkout" &&
        //                Math.Abs((a.CheckTime - checkOutTime).TotalMinutes) <= 1) ?? false;

        //            if (!hasCheckOut)
        //            {
        //                await _apiService.SendAsync(new AttendanceAPIDto
        //                {
        //                    employee_code = emp.HRMEmployeeCode,
        //                    type = "checkout",
        //                    date_time = checkOutTime
        //                });

        //                context.WriteLine(ConsoleTextColor.Green,
        //                    $"Check-out FIXED Extra for {emp.EmployeeName}, Checkout time : {checkOutTime}");
        //            }
        //        }

        //        // -----------------------------
        //        // STEP 5: AUTO CHECKOUT TIME
        //        // -----------------------------
        //        var autoCheckoutTime = lastSync.Date
        //            .AddHours(23)
        //            .AddMinutes(59);

        //        // -----------------------------
        //        // STEP 6: FILTER ELIGIBLE EMPLOYEES
        //        // -----------------------------
        //        var autoCheckoutEligibleEmps = employees
        //            .Where(e => !employeesToSkip.Contains(e.BiometricUserId))
        //            .ToList();

        //        // -----------------------------
        //        // STEP 7: FORCE AUTO CHECKOUT
        //        // -----------------------------
        //        foreach (var emp in autoCheckoutEligibleEmps)
        //        {
        //            if (string.IsNullOrEmpty(emp.HRMEmployeeCode))
        //                continue;

        //            var result = await _apiService.SendAsync(new AttendanceAPIDto
        //            {
        //                employee_code = emp.HRMEmployeeCode,
        //                type = "checkout",
        //                date_time = autoCheckoutTime
        //            });

        //            context.WriteLine(
        //                result ? ConsoleTextColor.Green : ConsoleTextColor.Red,
        //                $"Auto checkout {(result ? "SUCCESS" : "FAILED")} for {emp.EmployeeName}");
        //        }

        //        _logger.LogInformation("AutoCheckoutJob completed successfully");
        //        context.WriteLine(ConsoleTextColor.Green, "Auto checkout completed.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "AutoCheckoutJob failed");
        //        context.WriteLine(ConsoleTextColor.Red, ex.Message);
        //        throw;
        //    }
        //}
        #endregion

        public async Task Execute(PerformContext context)
        {
            try
            {
                var now = DateTime.Now;
                var today = now.Date;
                var oneHourAgo = now.AddHours(-1);
                var lastSync = _jobService.GetLastSyncTime();

                _logger.LogInformation(
                    "AutoCheckoutJob started at {Time}", now);

                context.WriteLine(
                    ConsoleTextColor.Cyan,
                    $"Auto checkout job started at {now:yyyy-MM-dd HH:mm:ss}");

                var biometricRecords = await _attendanceProvider.GetAttendanceAsync(lastSync);

                // ============================================================
                // 1. GET ALL ACTIVE AUTO CHECKOUT POLICIES
                // ============================================================

                var policyRules = await (
                    from master in _dbContext.AttendancePolicyMasters

                    join autoCheckoutRule in _dbContext.AttendancePolicyRules
                        on master.Id equals autoCheckoutRule.AttendancePolicyId

                    where master.IsEnable
                          && autoCheckoutRule.RuleCode ==
                             HRMConstants.SHIFT_AUTO_CHECKOUT_NAME
                          && autoCheckoutRule.RuleValue != null

                    select new
                    {
                        Policy = master,
                        AutoCheckoutTime = autoCheckoutRule.RuleValue
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

                        return new
                        {
                            Policy = policy.Policy,
                            ScheduledTime = scheduledTime
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

                foreach (var item in eligiblePolicies)
                {
                    var policy = item!.Policy;
                    var policyAutoCheckoutTime = item.ScheduledTime;

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
                            today,
                            context);
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
            DateTime today,
            PerformContext context)
        {
            if (string.IsNullOrWhiteSpace(employee.HRMEmployeeCode) ||
                string.IsNullOrWhiteSpace(employee.BiometricUserId))
                return;

            // Look at the ZYRA attendance logs for the current attendance date.
            // A successful biometric checkout already synchronized to ZYRA means
            // there is nothing left for this job to close.
            var attendanceLogs = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.EmployeeCode == employee.BiometricUserId &&
                    x.CheckTime >= today &&
                    x.CheckTime < today.AddDays(1) &&
                    x.IsProcessed &&
                    x.Status == "Success")
                .OrderByDescending(x => x.CheckTime)
                .ToListAsync();

            var latestCheckIn = attendanceLogs
                .Where(x => x.AttendanceState == "checkin")
                .OrderByDescending(x => x.CheckTime)
                .FirstOrDefault();

            var latestCheckout = attendanceLogs
                .Where(x =>
                    x.AttendanceState == "checkout" ||
                    x.AttendanceState == "Auto checkout")
                .OrderByDescending(x => x.CheckTime)
                .FirstOrDefault();

            if (latestCheckout != null &&
                (latestCheckIn == null || latestCheckout.CheckTime >= latestCheckIn.CheckTime))
            {
                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"{employee.EmployeeName} already checked out at {latestCheckout.CheckTime}");

                return;
            }

            if (latestCheckIn == null)
            {
                context.WriteLine(
                    ConsoleTextColor.Gray,
                    $"Auto checkout skipped for {employee.EmployeeName}: no open check-in found.");

                return;
            }

            // The policy time is only the point at which the employee becomes
            // eligible for auto checkout. It is not automatically used as the
            // final checkout when a later biometric punch exists.
            var latestPunch = await _attendanceProvider.GetLatestPunchAfterAsync(
                employee.BiometricUserId,
                latestCheckIn.CheckTime,
                DateTime.Now);

            if (latestPunch.HasValue)
            {
                var result = await _apiService.SendAsync(
                    new AttendanceAPIDto
                    {
                        employee_code = employee.HRMEmployeeCode,
                        type = "checkout",
                        date_time = latestPunch.Value
                    });

                _dbContext.AttendanceLogs.Add(new AttendanceLog
                {
                    EmployeeCode = employee.BiometricUserId,
                    CheckTime = latestPunch.Value,
                    AttendanceState = "checkout",
                    IsProcessed = true,
                    Status = result ? "Success" : "Failed",
                    ErrorMessage = result ? "Biometric checkout applied by auto checkout job" : "Failed to apply biometric checkout"
                });

                await _dbContext.SaveChangesAsync();

                context.WriteLine(
                    result ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                    $"Biometric checkout {(result ? "SUCCESS" : "FAILED")} for {employee.EmployeeName} at {latestPunch.Value:yyyy-MM-dd HH:mm:ss}");

                return;
            }

            // No punch after check-in exists yet, so fall back to the policy
            // auto-checkout time.
            var autoCheckoutResult = await _apiService.SendAsync(
                new AttendanceAPIDto
                {
                    employee_code = employee.HRMEmployeeCode,
                    type = "checkout",
                    date_time = policyAutoCheckoutTime
                });

            _dbContext.AttendanceLogs.Add(new AttendanceLog
            {
                EmployeeCode = employee.BiometricUserId,
                CheckTime = policyAutoCheckoutTime,
                AttendanceState = "Auto checkout",
                IsProcessed = true,
                Status = autoCheckoutResult ? "Success" : "Failed",
                ErrorMessage = autoCheckoutResult
                    ? "Policy auto checkout"
                    : "Failed to auto checkout"
            });

            await _dbContext.SaveChangesAsync();

            context.WriteLine(
                autoCheckoutResult ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                $"Auto checkout {(autoCheckoutResult ? "SUCCESS" : "FAILED")} for {employee.EmployeeName} using policy {policy.PolicyName} at {policyAutoCheckoutTime}");
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