using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public class AttendanceLogService : IAttendanceLogService
    {
        private readonly AttendanceDbContext _context;
        private readonly ILogger<AttendanceLogService> _logger;

        public AttendanceLogService(
            AttendanceDbContext context,
            ILogger<AttendanceLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(
            string employeeCode,
            DateTime checkTime,
            bool isSuccess,
            string attendanceState)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    _logger.LogWarning(
                        "Attempted to log attendance without an employee code.");
                    return;
                }

                var log = new AttendanceLog
                {
                    EmployeeCode = employeeCode,
                    IsProcessed = isSuccess,
                    ProcessedAt = DateTime.Now,
                    Status = isSuccess ? "Success" : "Failed",
                    AttendanceState = attendanceState ?? string.Empty,
                    CheckTime = checkTime,
                    ErrorMessage = isSuccess
                        ? null
                        : $"Failed to process {attendanceState}."
                };

                _context.AttendanceLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Attendance log saved for Employee {EmployeeCode} | State: {State} | Status: {Status} | CheckTime: {CheckTime}",
                    employeeCode,
                    attendanceState,
                    log.Status,
                    checkTime);
            }
            catch (Exception ex)
            {
                // Logging failure should not break the main attendance job.
                _logger.LogError(
                    ex,
                    "Failed to save attendance log for Employee {EmployeeCode}",
                    employeeCode);
            }
        }
    }
}
