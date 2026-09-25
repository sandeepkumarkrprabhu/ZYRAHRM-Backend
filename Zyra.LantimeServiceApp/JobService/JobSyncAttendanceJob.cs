using Cronos;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Zyra.LantimeServiceApp.Constants;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;
using ZyraHangfireModels.ServiceModels;

namespace Zyra.LantimeServiceApp.JobService
{
    public class JobSyncAttendanceJob : ISyncAttendanceJob
    {
        private readonly IEmployeeService _employeeService;
        private readonly IAttendanceProvider _attendanceProvider;
        private readonly IAttendanceProcessor _processor;
        private readonly IJobService _jobService;
        private readonly ILogger<JobSyncAttendanceJob> _logger;
        private readonly BiometricSyncSettings _settings;
        private readonly AttendanceSettings _attSettings;
        private readonly AttendanceDbContext _dbContext;

        private readonly IHttpService _httpService;
        private readonly IAttendanceDbService _attendanceDbService;

        private TimeSpan _checkOutStart;
        private TimeSpan _checkOutEnd;

        public JobSyncAttendanceJob(
            IEmployeeService employeeService,
            IAttendanceProvider attendanceProvider,
            IAttendanceProcessor processor,
            IJobService jobService,
            IOptions<BiometricSyncSettings> options,
            IOptions<AttendanceSettings> attSettings,
            AttendanceDbContext dbContext,
            IHttpService httpService,
            IAttendanceDbService attendanceDbService,
            ILogger<JobSyncAttendanceJob> logger)
        {
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _attendanceProvider = attendanceProvider ?? throw new ArgumentNullException(nameof(attendanceProvider));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = options.Value ?? throw new ArgumentNullException(nameof(options));
            _attSettings = attSettings.Value ?? throw new ArgumentNullException(nameof(attSettings));
            _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
            _attendanceDbService = attendanceDbService ?? throw new ArgumentNullException(nameof(attendanceDbService));
        }

        public async Task Execute(PerformContext context)
        {
            await SyncCheckInAttendance(context);
            await SyncCheckOutAttendance(context);
        }



        #region Attendance Sync Methods
        private async Task SyncCheckInAttendance(PerformContext context)
        {
            try
            {

                var lastSyncTime = GetLastSyncTime();

                LogInformation(context, $"Employee attendance check-in sync started at {DateTime.Now}");

                var records =
                            await _attendanceDbService.GetAttendanceAsync(lastSyncTime);

                LogInformation(context,
                    $"Total employee records fetched from Biometric: {records.Count}");

                // ---------------------------------------------------------
                // 1. Load employee mappings once
                // ---------------------------------------------------------

                var biometricUserIds = records
                    .Select(x => x.EmployeeCode)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                LogInformation(context,
                    $"Total unique biometric user IDs are: {string.Join(", ", biometricUserIds)}");

                var employeeMap =
                    await GetEmployeeMappingsAsync(biometricUserIds);

                // ---------------------------------------------------------
                // 2. Load attendance policies once
                // ---------------------------------------------------------

                var employeeIds = employeeMap.Values
                    .Select(x => x.UserId)
                    .Distinct()
                    .ToList();

                var policyMap =
                    await GetEmployeeAttendancePoliciesAsync(employeeIds);

                // ---------------------------------------------------------
                // 3. Process attendance records
                // ---------------------------------------------------------

                foreach (var record in records)
                {
                    try
                    {
                        await ProcessCheckInRecordAsync(
                            record,
                            employeeMap,
                            policyMap,
                            context);
                    }
                    catch (Exception ex)
                    {
                        var message =
                            $"Error processing attendance for {record.EmployeeName}";

                        _logger.LogError(ex, message);

                        context?.WriteLine(
                            ConsoleTextColor.Red,
                            $"{message}. Error: {ex.Message}");
                    }
                }

                // UpdateLastSyncTime(DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Attendance check-in sync failed");

                throw;
            }
        }

        private async Task SyncCheckOutAttendance(PerformContext context)
        {
            try
            {
                var lastSyncTime = GetLastSyncTime();

                _logger.LogInformation(
                    "Employee attendance check-out sync started at {Time}",
                    DateTime.Now);

                var records =
                    await _attendanceDbService.GetAttendanceAsync(lastSyncTime);

                _logger.LogInformation(
                    "Total employee records fetched from Biometric: {Count}",
                    records.Count);

                // 1. Load employee mappings once
                var biometricUserIds = records
                    .Select(x => x.EmployeeCode)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                var employeeMap =
                    await GetEmployeeMappingsAsync(biometricUserIds);

                _logger.LogInformation(
                    "Total unique biometric user IDs are: {Count}. IDs: {Ids}",
                    biometricUserIds.Count,
                    string.Join(", ", biometricUserIds));

                // 2. Load attendance policies once
                var employeeIds = employeeMap.Values
                    .Select(x => x.UserId)
                    .Distinct()
                    .ToList();

                var policyMap =
                    await GetEmployeeAttendancePoliciesAsync(employeeIds);

                // 3. Check Already Processed Checkout records
                var processedCheckouts = await GetProcessedCheckoutAsync(employeeIds);

                // 4. Process records
                foreach (var record in records)
                {
                    try
                    {
                        await ProcessCheckOutRecordAsync(
                            record,
                            employeeMap,
                            policyMap,
                            context);
                    }
                    catch (Exception ex)
                    {
                        var message =
                            $"Error processing check-out attendance for " +
                            $"{record.EmployeeName}";

                        _logger.LogError(ex, message);

                        context?.WriteLine(
                            ConsoleTextColor.Red,
                            $"{message}. Error: {ex.Message}");
                    }
                }

                // UpdateLastSyncTime(DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Attendance check-out sync failed");

                throw;
            }
        }

        #endregion

        #region Old Code (Commented Out)
        //public async Task Execute(PerformContext context)
        //{
        //    try
        //    {
        //        _checkOutStart = _attSettings.CheckOutStart;
        //        _checkOutEnd = _attSettings.CheckOutEnd;

        //        var lastSync = _jobService.GetLastSyncTime();

        //        var cron = _settings.AttendanceSyncJobCron;
        //        var timeInterval = GetIntervalFromCron(cron);
        //        _logger.LogInformation("SyncAttendanceJob started at {Time}", DateTime.Now);
        //        context.WriteLine(ConsoleTextColor.Cyan, "Attendance sync started...");

        //        // -----------------------------
        //        // STEP 1: FETCH DATA
        //        // -----------------------------
        //        var records = await _attendanceProvider.GetAttendanceAsync(lastSync);
        //        var employees = await _employeeService.GetEmployeesForSync();

        //        if (records == null || employees == null || records?.Count == 0 || employees?.Count == 0)
        //        {
        //            _logger.LogWarning("No data found for sync job");
        //            context.WriteLine(ConsoleTextColor.Cyan, "No data found for sync job.");
        //            return;
        //        }

        //        // -----------------------------
        //        // STEP 2: TIME WINDOW CHECK (CHECKOUT RULE)
        //        // -----------------------------
        //        var nowTime = _jobService.GetDayTimespan();
        //        bool IsCheckOutFilterApplicable = false;
        //        if (nowTime < _checkOutStart || nowTime > _checkOutEnd)
        //        {
        //            IsCheckOutFilterApplicable = false;
        //            _logger.LogInformation("Outside checkout window. Skipping processing.");
        //            context.WriteLine(ConsoleTextColor.Yellow, "Outside checkout window. Skipped.");
        //        }
        //        else
        //        {
        //            IsCheckOutFilterApplicable = true;
        //        }

        //        // Optional: cron-based sliding window
        //        var interval = timeInterval ?? TimeSpan.FromMinutes(30);
        //        var baseDate = new DateTime(1900, 1, 1);

        //        var windowStart = baseDate.Add(nowTime - interval);
        //        var windowEnd = baseDate.Add(nowTime);

        //        // -----------------------------
        //        // STEP 3: OPTIMIZE LOOKUP
        //        // -----------------------------
        //        var recordMap = records?
        //            .Where(r => !string.IsNullOrEmpty(r.EmployeeCode))
        //            .ToDictionary(x => x.EmployeeCode);

        //        // -----------------------------
        //        // STEP 4: FILTER ELIGIBLE EMPLOYEES FOR CHECKOUT
        //        // -----------------------------
        //        var eligibleEmployees = employees;
        //        if (IsCheckOutFilterApplicable)
        //        {
        //            var empCheckoutStart = new DateTime(1900, 01, 01).Add(_checkOutStart);
        //            eligibleEmployees = employees.Where(f => f.LastCheckoutFinal >= empCheckoutStart && f.LastCheckoutFinal <= windowEnd).ToList();

        //            _logger.LogInformation("Eligible Employees: {Employees}", JsonSerializer.Serialize(eligibleEmployees));
        //            context.WriteLine(ConsoleTextColor.Black, $"Eligible Employees for checkout. {string.Join(", ", eligibleEmployees.Select(e => e.BiometricUserId))}");
        //        }
        //        // -----------------------------
        //        // STEP 5: PROCESS ONLY ELIGIBLE EMPLOYEES
        //        // -----------------------------
        //        if (eligibleEmployees?.Count <= 0)
        //        {
        //            _logger.LogInformation("No employees found for syncAttendanceJob to process.");
        //            context.WriteLine(ConsoleTextColor.Cyan, "No employees found for syncAttendanceJob to process.");
        //        }
        //        else
        //        {
        //            foreach (var emp in eligibleEmployees)
        //            {
        //                if (recordMap.TryGetValue(emp.BiometricUserId, out var attendance))
        //                {
        //                    //Add logic for check if already processed successfully for today (checkin or checkout based on attendanceType)
        //                    var attendanceState = (IsCheckOutFilterApplicable ? "Checkout" : "checkin");

        //                    var punchStatus = await IsAlreadyProcessedAsync(emp.BiometricUserId, (IsCheckOutFilterApplicable ? attendance.CheckOutTime : attendance.CheckInTime), attendanceState);
        //                    if (!punchStatus)
        //                    {
        //                        _logger.LogInformation($"Punch time for, {emp.EmployeeName}, attendance state: {attendanceState} , processing Request.");
        //                        await _processor.ProcessAsync(emp, attendance, context, interval);
        //                    }
        //                    else {
        //                        _logger.LogInformation($"Punch time for, {emp.EmployeeName}, attendance state: {attendanceState} , already processed.");
        //                        context.WriteLine(ConsoleTextColor.Black, $"Punch time for , {emp.EmployeeName}, state: {attendanceState} , already processed.");
        //                    }
        //                }
        //                else
        //                {
        //                    _logger.LogInformation($"No employees punch time found for, {emp.EmployeeName} ,syncAttendanceJob to process.");
        //                    context.WriteLine(ConsoleTextColor.Cyan, $"No employees punch time found, {emp.EmployeeName}, for syncAttendanceJob to process.");
        //                }

        //            }
        //        }
        //        _logger.LogInformation("SyncAttendanceJob completed successfully");
        //        context.WriteLine(ConsoleTextColor.Green, "Attendance sync completed.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "SyncAttendanceJob failed");
        //        context.WriteLine(ConsoleTextColor.Red, ex.Message);
        //        throw;
        //    }
        //}
        #endregion


        private async Task<List<AttendanceLog>> GetProcessedCheckoutAsync(List<int> employeeIds)
        {
            var employeeCodes = employeeIds
                .Select(x => x.ToString())
                .ToList();

            var processedCheckouts = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .Where(f =>
                    employeeCodes.Contains(f.EmployeeCode) &&
                    f.AttendanceState == "checkout" &&
                    f.Status == "Success")
                .ToListAsync();

            return processedCheckouts;
        }

        private async Task<bool> IsAlreadyProcessedAsync(string employeeCode, DateTime punchTime, string attendanceState)
        {
            var date = punchTime.Date;

            return await _dbContext.AttendanceLogs
                .AnyAsync(x =>
                    x.EmployeeCode == employeeCode &&
                    x.CheckTime.Date == date &&
                    x.AttendanceState == attendanceState &&
                    x.IsProcessed == true);
        }

        private static TimeSpan? GetIntervalFromCron(string cron)
        {
            var expression = CronExpression.Parse(cron);

            var now = DateTime.UtcNow;

            var next = expression.GetNextOccurrence(now);
            if (next == null)
                return null;

            var nextNext = expression.GetNextOccurrence(next.Value);
            if (nextNext == null)
                return null;

            return nextNext.Value - next.Value;
        }

        #region Private Helper Methods
        private DateTime GetLastSyncTime()
        {
            return new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day); // Replace with DB value
                                                                                                //return new DateTime(2026,03,02);
        }

        private async Task<bool> SaveAttendance(AttendanceAPIDto employeeAttendance, PerformContext? context)
        {
            var url = new Uri(new Uri("https://pc.pumexinfotech.com/"), "api/attendance/action");

            try
            {
                await _httpService.PostAsync(url.ToString(), employeeAttendance);
                return true;
            }
            catch (Exception ex)
            {
                ApiResponseModel? apiResponse = null;

                try
                {
                    var jsonStartIndex = ex.Message.IndexOf('{');

                    if (jsonStartIndex >= 0)
                    {
                        var json = ex.Message.Substring(jsonStartIndex);

                        apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(
                            json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                    }
                }
                catch (JsonException)
                {
                    // JSON response could not be deserialized.
                }

                var errorMessage = apiResponse != null
                    ? $"{apiResponse.Message} - {apiResponse.Error}"
                    : ex.Message;

                var message =
                    $"Biometric attendance update failed for : " +
                    $"{employeeAttendance.employee_code} dated : " +
                    $"{employeeAttendance.date_time:dd-MM-yyyy HH:mm:ss} : " +
                    $"Error : {errorMessage}";

                LogInformation(
                    context,
                    message,
                    ConsoleTextColor.Red);

                return false;
            }
        }

        private async Task ProcessCheckInRecordAsync(AttendanceDto record, Dictionary<string, EmployeeMapperDto> employeeMap, Dictionary<int, EmployeeAttendancePolicy> policyMap, PerformContext? context)
        {
            // ---------------------------------------------------------
            // 1. Get employee mapping
            // ---------------------------------------------------------

            if (!employeeMap.TryGetValue(
                    record.EmployeeCode,
                    out var employee))
            {
                LogInformation(
                    context,
                    $"Biometric employee mapping not found for " +
                    $"{record.EmployeeName}. " +
                    $"Biometric code: {record.EmployeeCode}", ConsoleTextColor.Red);

                return;
            }

            // ---------------------------------------------------------
            // 2. Get attendance policy
            // ---------------------------------------------------------

            if (!policyMap.TryGetValue(
                    employee.UserId,
                    out var attendancePolicy))
            {
                LogInformation(
                    context,
                    $"Check-in skipped for {record.EmployeeName}. " +
                    $"No attendance policy configured for employee.");

                return;
            }

            // ---------------------------------------------------------
            // 3. Build shift window
            // ---------------------------------------------------------

            var shiftWindow =
                BuildShiftWindow(
                    attendancePolicy,
                    record.CheckInTime,
                    record.EmployeeName);

            if (shiftWindow == null)
            {
                return;
            }

            // ---------------------------------------------------------
            // 4. Validate biometric check-in
            // ---------------------------------------------------------

            if (!IsValidCheckInTime(
                    record.CheckInTime,
                    shiftWindow,
                    record.EmployeeName,
                    context))
            {
                return;
            }

            // ---------------------------------------------------------
            // 5. Check duplicate check-in
            // ---------------------------------------------------------

            var alreadyCheckedIn =
                await IsAlreadyCheckedInAsync(
                    employee.EmployeeCode,
                    shiftWindow.Start,
                    shiftWindow.End);

            if (alreadyCheckedIn)
            {
                LogInformation(
                    context,
                    $"Check-in skipped for {record.EmployeeName}. " +
                    $"Employee has already checked in successfully. " +
                    $"Punch Time: {record.CheckInTime:dd-MM-yyyy HH:mm:ss}, " +
                    $"Shift Window: {shiftWindow.Start:dd-MM-yyyy HH:mm} - " +
                    $"{shiftWindow.End:dd-MM-yyyy HH:mm}");

                return;
            }

            // ---------------------------------------------------------
            // 6. Send attendance to ZYRA
            // ---------------------------------------------------------

            await SendCheckInAttendanceAsync(
                employee,
                record,
                shiftWindow,
                context);
        }

        private async Task ProcessCheckOutRecordAsync(AttendanceDto record, Dictionary<string, EmployeeMapperDto> employeeMap, Dictionary<int, EmployeeAttendancePolicy> policyMap, PerformContext? context)
        {
            // Employee mapping
            if (!employeeMap.TryGetValue(
                    record.EmployeeCode,
                    out var employee))
            {
                LogInformation(
                    context,
                    $"Biometric employee mapping not found for " +
                    $"{record.EmployeeName}. " +
                    $"Biometric code: {record.EmployeeCode}");

                return;
            }

            // Attendance policy
            if (!policyMap.TryGetValue(
                    employee.UserId,
                    out var attendancePolicy))
            {
                LogInformation(
                    context,
                    $"Check-out skipped for {record.EmployeeName}. " +
                    $"No attendance policy configured for employee.");

                return;
            }

            // Build shift window based on the employee's policy
            var shiftWindow =
                BuildShiftWindow(
                    attendancePolicy,
                    record.CheckOutTime,
                    record.EmployeeName);

            if (shiftWindow == null)
            {
                return;
            }

            // Validate checkout against shift end
            if (!IsValidCheckOutTime(
                    record.CheckOutTime,
                    shiftWindow,
                    record.EmployeeName,
                    context))
            {
                return;
            }

            await SendCheckOutAttendanceAsync(
                employee,
                record,
                shiftWindow,
                context);
        }

        private async Task<Dictionary<string, EmployeeMapperDto>> GetEmployeeMappingsAsync(IEnumerable<string> biometricUserIds)
        {
            var ids = biometricUserIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<string, EmployeeMapperDto>();
            }

            var employees = await _dbContext.EmployeeMappings
                .AsNoTracking()
                .Where(x => ids.Contains(x.BiometricUserId) && !x.IsExcludeFromBiometric && x.IsActive)
                .Select(x => new EmployeeMapperDto
                {
                    UserId = x.Id,
                    EmployeeCode = x.HRMEmployeeCode,
                    EmployeeName = x.EmployeeName,
                    BiometricUserId = x.BiometricUserId
                })
                .ToListAsync();

            return employees
                .Where(x => !string.IsNullOrWhiteSpace(x.BiometricUserId))
                .GroupBy(x => x.BiometricUserId!)
                .ToDictionary(
                    x => x.Key,
                    x => x.First());
        }

        private async Task<Dictionary<int, EmployeeAttendancePolicy>> GetEmployeeAttendancePoliciesAsync(IEnumerable<int> employeeIds)
        {
            var ids = employeeIds
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<int, EmployeeAttendancePolicy>();
            }

            var policies = await _dbContext.EmployeeAttendancePolicies
                .AsNoTracking()
                .Include(x => x.AttendancePolicy)
                    .ThenInclude(x => x.Rules)
                .Where(x =>
                    ids.Contains(x.EmployeeId) &&
                    x.IsEnabled &&
                    x.AttendancePolicy != null &&
                    x.AttendancePolicy.IsEnable)
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync();

            return policies
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(
                    x => x.Key,
                    x => x.First());
        }

        private ShiftWindow? BuildShiftWindow(EmployeeAttendancePolicy employeeShift, DateTime checkInTime, string employeeName)
        {
            var rules = employeeShift.AttendancePolicy?.Rules;

            var shiftStartValue = rules?
                .FirstOrDefault(x =>
                    x.RuleCode == HRMConstants.SHIFT_START_TIME_NAME)
                ?.RuleValue;

            var shiftEndValue = rules?
                .FirstOrDefault(x =>
                    x.RuleCode == HRMConstants.SHIFT_END_TIME_NAME)
                ?.RuleValue;

            if (!TimeSpan.TryParse(shiftStartValue, out var shiftStart))
            {
                _logger.LogWarning(
                    "Invalid shift start time for {EmployeeName}: {ShiftStart}",
                    employeeName,
                    shiftStartValue);

                return null;
            }

            if (!TimeSpan.TryParse(shiftEndValue, out var shiftEnd))
            {
                _logger.LogWarning(
                    "Invalid shift end time for {EmployeeName}: {ShiftEnd}",
                    employeeName,
                    shiftEndValue);

                return null;
            }

            // ---------------------------------------------------------
            // Determine overnight shift
            // ---------------------------------------------------------

            var isOvernightShift = shiftEnd <= shiftStart;

            var shiftEndTime = shiftEnd;

            if (isOvernightShift)
            {
                shiftEnd = shiftEnd.Add(TimeSpan.FromDays(1));
            }

            var shiftDate = checkInTime.Date;

            if (isOvernightShift &&
                checkInTime.TimeOfDay < shiftEndTime)
            {
                shiftDate = shiftDate.AddDays(-1);
            }

            // ---------------------------------------------------------
            // Grace period
            // ---------------------------------------------------------

            var gracePeriodValue = rules?
                .FirstOrDefault(x =>
                    x.RuleCode == HRMConstants.SHIFT_GRACE_PERIOD_NAME)
                ?.RuleValue;

            var gracePeriod = TimeSpan.Zero;

            if (!string.IsNullOrWhiteSpace(gracePeriodValue))
            {
                if (!TimeSpan.TryParse(
                        gracePeriodValue,
                        out gracePeriod))
                {
                    _logger.LogWarning(
                        "Invalid grace period for {EmployeeName}: {GracePeriod}. " +
                        "Using 00:00.",
                        employeeName,
                        gracePeriodValue);

                    gracePeriod = TimeSpan.Zero;
                }
            }

            var effectiveShiftStart =
                shiftStart.Subtract(gracePeriod);

            var effectiveShiftEnd =
                shiftEnd.Add(gracePeriod);

            return new ShiftWindow
            {
                // Check-in validation window
                Start = shiftDate.Add(shiftStart.Subtract(gracePeriod)),

                End = shiftDate.Add(shiftEnd.Add(gracePeriod)),

                // Actual shift times
                ShiftStart = shiftDate.Add(shiftStart),
                ShiftEnd = shiftDate.Add(shiftEnd),

                IsOvernight = isOvernightShift
            };
        }

        private bool IsValidCheckInTime(DateTime checkInTime, ShiftWindow shiftWindow, string employeeName, PerformContext? context)
        {
            if (checkInTime >= shiftWindow.Start &&
                checkInTime <= shiftWindow.End)
            {
                return true;
            }

            var message =
                $"Check-in skipped for {employeeName}. " +
                $"Check-in Time: {checkInTime:dd-MM-yyyy HH:mm:ss}, " +
                $"Valid Shift Window: " +
                $"{shiftWindow.Start:dd-MM-yyyy HH:mm} - " +
                $"{shiftWindow.End:dd-MM-yyyy HH:mm}";

            LogInformation(context, message, ConsoleTextColor.Yellow);

            return false;
        }

        private bool IsValidCheckOutTime(DateTime checkOutTime, ShiftWindow shiftWindow, string employeeName, PerformContext? context)
        {
            if (checkOutTime >= shiftWindow.ShiftEnd &&
                checkOutTime <= shiftWindow.End)
            {
                return true;
            }

            var message =
                $"Check-out skipped for {employeeName}. " +
                $"Check-out Time: {checkOutTime:dd-MM-yyyy HH:mm:ss}, " +
                $"Valid Check-out Window: " +
                $"{shiftWindow.ShiftEnd:dd-MM-yyyy HH:mm} - " +
                $"{shiftWindow.End:dd-MM-yyyy HH:mm}";

            LogInformation(context, message, ConsoleTextColor.Yellow);

            return false;
        }

        private async Task<bool> IsAlreadyCheckedInAsync(string employeeCode, DateTime shiftStart, DateTime shiftEnd)
        {
            return await _dbContext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == employeeCode &&
                    x.Status == "Success" &&
                    x.CheckTime >= shiftStart &&
                    x.CheckTime <= shiftEnd);
        }

        private async Task SendCheckInAttendanceAsync(EmployeeMapperDto employee, AttendanceDto record, ShiftWindow shiftWindow, PerformContext? context)
        {
            var message =
                $"Attendance sync for employee {record.EmployeeName}, " +
                $"check-in: {record.CheckInTime:dd-MM-yyyy HH:mm:ss}, " +
                $"Shift Window: " +
                $"{shiftWindow.Start:dd-MM-yyyy HH:mm} - " +
                $"{shiftWindow.End:dd-MM-yyyy HH:mm}";

            LogInformation(context, message, ConsoleTextColor.DarkYellow);

            var request = new AttendanceAPIDto
            {
                employee_code = employee.EmployeeCode,
                type = "checkin",
                date_time = record.CheckInTime
            };

            var actionStatus =
                await SaveAttendance(request, context);

            if (actionStatus)
            {
                message =
                    $"Biometric attendance updated successfully " +
                    $"for {record.EmployeeName}";

                LogInformation(context, message, ConsoleTextColor.DarkGreen);
            }
            else
            {
                message =
                    $"Biometric attendance update failed " +
                    $"for {record.EmployeeName}";

                LogInformation(context, message, ConsoleTextColor.White);
            }
        }

        private async Task SendCheckOutAttendanceAsync(EmployeeMapperDto employee, AttendanceDto record, ShiftWindow shiftWindow, PerformContext? context)
        {
            var message =
                $"Attendance sync for employee {record.EmployeeName}, " +
                $"check-out: {record.CheckOutTime:dd-MM-yyyy HH:mm:ss}, " +
                $"Shift End: {shiftWindow.End:dd-MM-yyyy HH:mm}";

            LogInformation(context, message, ConsoleTextColor.DarkGreen);

            var request = new AttendanceAPIDto
            {
                employee_code = employee.EmployeeCode,
                type = "checkOut",
                date_time = record.CheckOutTime
            };

            var actionStatus =
                await SaveAttendance(request, context);

            if (actionStatus)
            {
                message =
                    $"Biometric check-out attendance updated successfully " +
                    $"for {record.EmployeeName}";

                LogInformation(context, message, ConsoleTextColor.DarkGreen);

            }
            else
            {
                message =
                    $"Biometric check-out attendance update failed " +
                    $"for {record.EmployeeName}";

                LogInformation(context, message, ConsoleTextColor.DarkGreen);
            }
        }

        private void LogInformation(PerformContext? context, string message, ConsoleTextColor? color = null)
        {
            _logger.LogInformation(message);

            context?.WriteLine(
                color ?? ConsoleTextColor.Yellow,
                message);
        }
        #endregion
    }
}
