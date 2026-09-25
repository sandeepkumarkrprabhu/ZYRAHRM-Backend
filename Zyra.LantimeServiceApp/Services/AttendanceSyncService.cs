using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Services
{
    public sealed class AttendanceSyncService : IAttendanceSyncService
    {
        private readonly IEmployeeAttendanceService _employeeAttendanceService;
        private readonly IAttendanceDbService _attendanceDbService;
        private readonly IAttendanceApiService _attendanceApiService;
        private readonly IAttendanceLogService _attendanceLogService;
        private readonly IAttendanceValidationService _attendanceValidationService;
        private readonly ILogger<AttendanceSyncService> _logger;

        public AttendanceSyncService(
            IAttendanceDbService attendanceDbService,
            IAttendanceApiService attendanceApiService,
            IAttendanceLogService attendanceLogService,
            IEmployeeAttendanceService employeeAttendanceService,
            IAttendanceValidationService attendanceValidationService,
            ILogger<AttendanceSyncService> logger)
        {
            _attendanceDbService = attendanceDbService ?? throw new ArgumentNullException(nameof(attendanceDbService));
            _attendanceApiService = attendanceApiService ?? throw new ArgumentNullException(nameof(attendanceApiService));
            _attendanceLogService = attendanceLogService ?? throw new ArgumentNullException(nameof(attendanceLogService));
            _employeeAttendanceService = employeeAttendanceService ?? throw new ArgumentNullException(nameof(employeeAttendanceService));
            _attendanceValidationService = attendanceValidationService ?? throw new ArgumentNullException(nameof(attendanceValidationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SyncCheckInAsync(PerformContext? context = null)
        {
            await SyncAsync(false, context);
        }

        public async Task SyncCheckOutAsync(PerformContext? context = null)
        {
            await SyncAsync(true, context);
        }

        private async Task SyncAsync(bool checkout, PerformContext? context)
        {
            try
            {
                var syncTime = DateTime.Today;

                Log(context,
                    $"Employee attendance {(checkout ? "check-out" : "check-in")} sync started at {DateTime.Now}");

                var records = await _attendanceDbService.GetAttendanceAsync(syncTime);

                if (records == null || records.Count == 0)
                {
                    Log(context, "No biometric attendance records found.", ConsoleTextColor.Cyan);
                    return;
                }

                var biometricIds = records
                    .Select(x => x.EmployeeCode)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                var employeeMap = await _employeeAttendanceService.GetEmployeeMappingsAsync(biometricIds);

                var employeeIds = employeeMap.Values
                    .Select(x => x.UserId)
                    .Distinct()
                    .ToList();

                var policyMap = await _employeeAttendanceService.GetEmployeeAttendancePoliciesAsync(employeeIds);

                foreach (var record in records)
                {
                    try
                    {
                        if (checkout)
                            await ProcessCheckOutRecordAsync(record, employeeMap, policyMap, context);
                        else
                            await ProcessCheckInRecordAsync(record, employeeMap, policyMap, context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error processing attendance for {EmployeeName}",
                            record.EmployeeName);

                        Log(
                            context,
                            $"Error processing attendance for {record.EmployeeName}: {ex.Message}",
                            ConsoleTextColor.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Attendance {AttendanceType} sync failed",
                    checkout ? "check-out" : "check-in");

                throw;
            }
        }

        private async Task ProcessCheckInRecordAsync(
            AttendanceDto record,
            Dictionary<string, EmployeeMapperDto> employeeMap,
            Dictionary<int, EmployeeAttendancePolicy> policyMap,
            PerformContext? context)
        {
            if (!employeeMap.TryGetValue(record.EmployeeCode, out var employee))
            {
                Log(context,
                    $"Biometric employee mapping not found for {record.EmployeeName}. " +
                    $"Biometric code: {record.EmployeeCode}",
                    ConsoleTextColor.Red);
                return;
            }

            if (!policyMap.TryGetValue(employee.UserId, out var policy))
            {
                Log(context,
                    $"Check-in skipped for {record.EmployeeName}. No attendance policy configured.");
                return;
            }

            var validation = await _attendanceValidationService.ValidateCheckInAsync(
                employee,
                policy,
                record);

            if (!validation.IsValid)
            {
                Log(
                    context,
                    $"Check-in skipped for {record.EmployeeName}. {validation.Reason}",
                    ConsoleTextColor.Yellow);
                return;
            }

            var request = new AttendanceAPIDto
            {
                employee_code = employee.EmployeeCode,
                type = "checkin",
                date_time = record.CheckInTime
            };

            var success = await _attendanceApiService.SendAsync(request);

            await _attendanceLogService.LogAsync(
                record,
                success,
                request.type ?? "checkin");

            Log(
                context,
                $"Biometric check-in {(success ? "updated successfully" : "update failed")} for {record.EmployeeName}",
                success ? ConsoleTextColor.Green : ConsoleTextColor.Red);
        }

        private async Task ProcessCheckOutRecordAsync(
            AttendanceDto record,
            Dictionary<string, EmployeeMapperDto> employeeMap,
            Dictionary<int, EmployeeAttendancePolicy> policyMap,
            PerformContext? context)
        {
            if (!employeeMap.TryGetValue(record.EmployeeCode, out var employee))
            {
                Log(context,
                    $"Biometric employee mapping not found for {record.EmployeeName}. " +
                    $"Biometric code: {record.EmployeeCode}");
                return;
            }

            if (!policyMap.TryGetValue(employee.UserId, out var policy))
            {
                Log(context,
                    $"Check-out skipped for {record.EmployeeName}. No attendance policy configured.");
                return;
            }

            var validation = _attendanceValidationService.ValidateCheckOut(
                employee,
                policy,
                record);

            if (!validation.IsValid)
            {
                Log(
                    context,
                    $"Check-out skipped for {record.EmployeeName}. {validation.Reason}",
                    ConsoleTextColor.Yellow);
                return;
            }

            var request = new AttendanceAPIDto
            {
                employee_code = employee.EmployeeCode,
                type = "checkOut",
                date_time = record.CheckOutTime
            };

            var success = await _attendanceApiService.SendAsync(request);

            await _attendanceLogService.LogAsync(
                record,
                success,
                request.type ?? "checkOut");

            Log(
                context,
                $"Biometric check-out {(success ? "updated successfully" : "update failed")} for {record.EmployeeName}",
                success ? ConsoleTextColor.Green : ConsoleTextColor.Red);
        }

        private void Log(PerformContext? context, string message)
        {
            Log(context, message, ConsoleTextColor.Yellow);
        }

        private void Log(PerformContext? context,string message, ConsoleTextColor color)
        {
            _logger.LogInformation(message);
            context?.WriteLine(color, message);
        }
    }
}
