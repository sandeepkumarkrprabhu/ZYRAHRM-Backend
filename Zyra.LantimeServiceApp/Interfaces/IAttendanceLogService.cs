using System;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceLogService
    {
        /// <summary>
        /// Logs an attendance processing result using the exact employee code,
        /// timestamp, and attendance operation supplied by the caller.
        /// </summary>
        Task LogAsync(
            string employeeCode,
            DateTime checkTime,
            bool isSuccess,
            string attendanceState);
    }
}
