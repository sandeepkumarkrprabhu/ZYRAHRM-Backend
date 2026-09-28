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
            var today = now.Date;

            try
            {
                _logger.LogInformation("Company force checkout job started at {Time}", now);
                context.WriteLine(
                    ConsoleTextColor.Cyan,
                    $"Company force checkout started at {now:yyyy-MM-dd HH:mm:ss}");

                // Process only employees who currently have an open attendance.
                // The shift check below excludes employees whose assigned shift
                // is active at the time of company force checkout.
                var openEmployeeIds = await GetOpenEmployeeIdsAsync(today);

                if (openEmployeeIds.Count == 0)
                {
                    context.WriteLine(ConsoleTextColor.Gray, "No open attendance records found.");
                    return;
                }

                var policyMap = await _employeeAttendanceService
                    .GetEmployeeAttendancePoliciesAsync(openEmployeeIds);

                foreach (var employeeId in openEmployeeIds)
                {
                    if (!policyMap.TryGetValue(employeeId, out var assignment) ||
                        assignment.AttendancePolicy == null)
                    {
                        context.WriteLine(
                            ConsoleTextColor.Gray,
                            $"Force checkout skipped for employee {employeeId}: no active attendance policy.");
                        continue;
                    }

                    var employee = await _dbContext.EmployeeMappings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.Id == employeeId &&
                            x.IsActive &&
                            !x.IsExcludeFromBiometric);

                    if (employee == null)
                        continue;

                    var shift = _shiftService.BuildShiftWindow(
                        assignment,
                        now,
                        employee.EmployeeName);

                    if (shift == null)
                    {
                        context.WriteLine(
                            ConsoleTextColor.Gray,
                            $"Force checkout skipped for {employee.EmployeeName}: unable to determine shift.");
                        continue;
                    }

                    // If the assigned shift is active now, leave the employee
                    // open. This is especially important for overnight shifts.
                    if (now >= shift.ShiftStart && now <= shift.ShiftEnd)
                    {
                        context.WriteLine(
                            ConsoleTextColor.Yellow,
                            $"Force checkout excluded {employee.EmployeeName}: " +
                            $"active shift {shift.ShiftStart:yyyy-MM-dd HH:mm} - {shift.ShiftEnd:yyyy-MM-dd HH:mm}");
                        continue;
                    }

                    var latestCheckIn = await _dbContext.AttendanceLogs
                        .AsNoTracking()
                        .Where(x =>
                            x.EmployeeCode == employee.BiometricUserId &&
                            x.CheckTime >= today.AddDays(-1) &&
                            x.CheckTime <= now &&
                            x.IsProcessed &&
                            x.Status == "Success" &&
                            x.AttendanceState == "checkin")
                        .OrderByDescending(x => x.CheckTime)
                        .FirstOrDefaultAsync();

                    if (latestCheckIn == null)
                        continue;

                    var latestCheckoutExists = await _dbContext.AttendanceLogs
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.EmployeeCode == employee.BiometricUserId &&
                            x.CheckTime >= latestCheckIn.CheckTime &&
                            x.CheckTime <= now &&
                            x.IsProcessed &&
                            x.Status == "Success" &&
                            (x.AttendanceState == "checkout" ||
                             x.AttendanceState == "Auto checkout" ||
                             x.AttendanceState == "Force checkout"));

                    if (latestCheckoutExists)
                        continue;

                    // The biometric device is checked one final time before
                    // force checkout so an already-recorded late punch wins.
                    var latestPunch = await _attendanceProvider.GetLatestPunchAfterAsync(
                        employee.BiometricUserId,
                        latestCheckIn.CheckTime,
                        now);

                    var checkoutTime = latestPunch ?? now;
                    var checkoutState = latestPunch.HasValue ? "checkout" : "Force checkout";

                    var result = await _apiService.SendAsync(
                        new AttendanceAPIDto
                        {
                            employee_code = employee.HRMEmployeeCode,
                            type = "checkout",
                            date_time = checkoutTime
                        });

                    if (result)
                    {
                        _dbContext.AttendanceLogs.Add(new AttendanceLog
                        {
                            EmployeeCode = employee.BiometricUserId,
                            CheckTime = checkoutTime,
                            AttendanceState = checkoutState,
                            IsProcessed = true,
                            Status = "Success",
                            ErrorMessage = latestPunch.HasValue
                                ? "Latest biometric punch used by company force checkout job"
                                : "Company force checkout"
                        });

                        await _dbContext.SaveChangesAsync();
                    }

                    context.WriteLine(
                        result ? ConsoleTextColor.Green : ConsoleTextColor.Red,
                        $"{checkoutState} {(result ? "SUCCESS" : "FAILED")} " +
                        $"for {employee.EmployeeName} at {checkoutTime:yyyy-MM-dd HH:mm:ss}");
                }

                _logger.LogInformation("Company force checkout job completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Company force checkout job failed.");
                context.WriteLine(
                    ConsoleTextColor.Red,
                    $"Company force checkout failed: {ex.Message}");
                throw;
            }
        }

        private async Task<List<int>> GetOpenEmployeeIdsAsync(DateTime today)
        {
            var logs = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.CheckTime >= today.AddDays(-1) &&
                    x.CheckTime <= DateTime.Now &&
                    x.IsProcessed &&
                    x.Status == "Success")
                .Select(x => new
                {
                    x.EmployeeCode,
                    x.CheckTime,
                    x.AttendanceState
                })
                .ToListAsync();

            return logs
                .GroupBy(x => x.EmployeeCode)
                .Where(g =>
                {
                    var lastIn = g
                        .Where(x => x.AttendanceState == "checkin")
                        .OrderByDescending(x => x.CheckTime)
                        .FirstOrDefault();

                    var lastOut = g
                        .Where(x =>
                            x.AttendanceState == "checkout" ||
                            x.AttendanceState == "Auto checkout" ||
                            x.AttendanceState == "Force checkout")
                        .OrderByDescending(x => x.CheckTime)
                        .FirstOrDefault();

                    return lastIn != null &&
                           (lastOut == null || lastOut.CheckTime < lastIn.CheckTime);
                })
                .Join(
                    _dbContext.EmployeeMappings.AsNoTracking(),
                    log => log.Key,
                    employee => employee.BiometricUserId,
                    (log, employee) => employee.Id)
                .Distinct()
                .ToList();
        }
    }
}
