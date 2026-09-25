using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
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
            AttendanceDto attendance,
            bool isSuccess,
            string attendanceState)
        {
            try
            {
                if (attendance == null)
                {
                    _logger.LogWarning("Attempted to log null attendance");
                    return;
                }

                var checkTime = ResolveCheckTime(attendance, attendanceState);

                var log = new AttendanceLog
                {
                    EmployeeCode = attendance.EmployeeCode,
                    IsProcessed = isSuccess,
                    ProcessedAt = DateTime.Now,
                    Status = isSuccess ? "Success" : "Failed",
                    AttendanceState = attendanceState ?? string.Empty,
                    CheckTime = checkTime,
                    ErrorMessage = isSuccess ? null : $"Failed to process {attendanceState}."
                };

                _context.AttendanceLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Attendance log saved for Employee {EmployeeCode} | State: {State} | Status: {Status}",
                    attendance.EmployeeCode,
                    attendanceState,
                    log.Status);
            }
            catch (Exception ex)
            {
                // Logging failure should not break the main attendance job.
                _logger.LogError(
                    ex,
                    "Failed to save attendance log for Employee {EmployeeCode}",
                    attendance?.EmployeeCode);
            }
        }

        private static DateTime ResolveCheckTime(
            AttendanceDto attendance,
            string attendanceState)
        {
            var normalizedState = attendanceState?.Trim().ToLowerInvariant();

            return normalizedState switch
            {
                "checkin" or "extra checkin" =>
                    attendance.CheckInTime != DateTime.MinValue
                        ? attendance.CheckInTime
                        : DateTime.Now,

                "checkout" or "extra checkout" or "auto checkout" =>
                    attendance.CheckOutTime != DateTime.MinValue
                        ? attendance.CheckOutTime
                        : DateTime.Now,

                _ =>
                    attendance.CheckInTime != DateTime.MinValue
                        ? attendance.CheckInTime
                        : attendance.CheckOutTime != DateTime.MinValue
                            ? attendance.CheckOutTime
                            : DateTime.Now
            };
        }
    }
}