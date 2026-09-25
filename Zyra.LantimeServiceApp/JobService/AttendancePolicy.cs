using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public class AttendancePolicy : IAttendancePolicy
    {
        // You can later move these to config/appsettings
        private static readonly TimeSpan CheckInStart = new TimeSpan(8, 0, 0);
        private static readonly TimeSpan CheckInEnd = new TimeSpan(15, 0, 0);

        private static readonly TimeSpan CheckOutStart = new TimeSpan(18, 0, 0);
        private static readonly TimeSpan CheckOutEnd = new TimeSpan(23, 30, 0);

        public AttendanceAPIDto? BuildRequest(
            EmployeeMapping employee,
            AttendanceDto attendance, TimeSpan timeSpan)
        {
            //var now = DateTime.Now.TimeOfDay;
            var now = timeSpan;
            //var now = new TimeSpan(10, 0, 0); // 10:00 AM


            // -------------------------------
            // SAFETY CHECKS
            // -------------------------------
            if (attendance == null)
                return null;

            if (string.IsNullOrEmpty(employee.HRMEmployeeCode))
                return null;

            // -------------------------------
            // RULE 1: CHECK-IN WINDOW
            // -------------------------------
            if (IsCheckInWindow(now))
            {
                return BuildCheckIn(employee, attendance);
            }

            // -------------------------------
            // RULE 2: CHECK-OUT WINDOW
            // -------------------------------
            if (IsCheckOutWindow(now))
            {
                return BuildCheckOut(employee, attendance);
            }

            // -------------------------------
            // DEFAULT: NO ACTION
            // -------------------------------
            return null;
        }

        // =====================================================
        // CHECK-IN LOGIC
        // =====================================================
        private AttendanceAPIDto? BuildCheckIn(
            EmployeeMapping employee,
            AttendanceDto attendance)
        {
            if (attendance.CheckInTime == DateTime.MinValue)
                return null;

            return new AttendanceAPIDto
            {
                employee_code = employee.HRMEmployeeCode,
                type = "checkin",
                date_time = attendance.CheckInTime
            };
        }

        // =====================================================
        // CHECK-OUT LOGIC
        // =====================================================
        private AttendanceAPIDto? BuildCheckOut(
            EmployeeMapping employee,
            AttendanceDto attendance)
        {
            if (attendance.CheckOutTime == DateTime.MinValue)
                return null;

            if (attendance.CheckInTime == DateTime.MinValue)
                return null;

            if (attendance.CheckOutTime <= attendance.CheckInTime)
                return null;

            return new AttendanceAPIDto
            {
                employee_code = employee.HRMEmployeeCode,
                type = "checkout",
                date_time = attendance.CheckOutTime
            };
        }

        // =====================================================
        // TIME RULES
        // =====================================================
        private bool IsCheckInWindow(TimeSpan now)
        {
            return DateTime.Now.TimeOfDay >= CheckInStart && DateTime.Now.TimeOfDay <= CheckInEnd;
        }

        private bool IsCheckOutWindow(TimeSpan now)
        {
            return DateTime.Now.TimeOfDay >= CheckOutStart && DateTime.Now.TimeOfDay <= CheckOutEnd;
        }
    }

}
