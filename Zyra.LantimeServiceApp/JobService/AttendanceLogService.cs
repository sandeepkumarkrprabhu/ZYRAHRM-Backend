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

                var checkTime = ResolveCheckTime(attendance);
                var checkinStatus = IsCheckIn(checkTime);

                var log = new AttendanceLog
                {
                    EmployeeCode = attendance.EmployeeCode,
                    IsProcessed = isSuccess,
                    ProcessedAt = DateTime.Now,
                    Status = isSuccess ? "Success" : "Failed",
                    AttendanceState = attendanceState ?? string.Empty,
                    CheckTime = checkTime,
                    ErrorMessage = (checkinStatus == true ? "CheckIn" : "CheckOut")
                };

                _context.AttendanceLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Attendance log saved for Employee {EmployeeCode} | Status: {Status}",
                    attendance.EmployeeCode,
                    log.Status);
            }
            catch (Exception ex)
            {
                // IMPORTANT:
                // Logging failure should NOT break main job
                _logger.LogError(ex,
                    "Failed to save attendance log for Employee {EmployeeCode}",
                    attendance?.EmployeeCode);
            }
        }

        // -------------------------------------------------
        // SAFE CHECK TIME RESOLUTION
        // -------------------------------------------------
        private DateTime ResolveCheckTime(AttendanceDto attendance)
        {
            var time = DateTime.Now.TimeOfDay;

            // Same logic you had earlier but isolated cleanly
            if (time <= new TimeSpan(15, 0, 0))
            {
                return attendance.CheckInTime != DateTime.MinValue
                    ? attendance.CheckInTime
                    : DateTime.Now;
            }

            return attendance.CheckOutTime != DateTime.MinValue
                ? attendance.CheckOutTime
                : DateTime.Now;
        }

        private bool IsCheckIn(DateTime bioEntryTime)
        {
            var punchTime = bioEntryTime;
            // Same logic you had earlier but isolated cleanly
            if (punchTime.TimeOfDay <= new TimeSpan(15, 0, 0))
            {
                return true;
            }

            return false;
        }
    }
}
