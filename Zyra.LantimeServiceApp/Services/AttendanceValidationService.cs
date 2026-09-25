using Microsoft.EntityFrameworkCore;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;
using ZyraHangfireModels.ServiceModels;

namespace Zyra.LantimeServiceApp.Services
{
    public sealed class AttendanceValidationService : IAttendanceValidationService
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly IShiftService _shiftService;

        public AttendanceValidationService(
            AttendanceDbContext dbContext,
            IShiftService shiftService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _shiftService = shiftService ?? throw new ArgumentNullException(nameof(shiftService));
        }

        public async Task<AttendanceValidationResult> ValidateCheckInAsync(
            EmployeeMapperDto employee,
            EmployeeAttendancePolicy policy,
            AttendanceDto attendance)
        {
            var shift = _shiftService.BuildShiftWindow(
                policy,
                attendance.CheckInTime,
                attendance.EmployeeName);

            if (shift == null)
            {
                return Invalid("Unable to build the employee shift window.");
            }

            if (attendance.CheckInTime < shift.Start ||
                attendance.CheckInTime > shift.End)
            {
                return Invalid(
                    $"Check-in time {attendance.CheckInTime:dd-MM-yyyy HH:mm:ss} is outside the allowed shift window.");
            }

            var alreadyCheckedIn = await _dbContext.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeCode == employee.BiometricUserId &&
                    x.Status == "Success" &&
                    x.AttendanceState == "checkin" &&
                    x.CheckTime >= shift.Start &&
                    x.CheckTime <= shift.End);

            if (alreadyCheckedIn)
            {
                return Invalid("Employee has already checked in successfully for this shift.");
            }

            return Valid();
        }

        public AttendanceValidationResult ValidateCheckOut(
            EmployeeMapperDto employee,
            EmployeeAttendancePolicy policy,
            AttendanceDto attendance)
        {
            if (attendance.CheckOutTime == DateTime.MinValue)
            {
                return Invalid("No biometric check-out time was found.");
            }

            var shift = _shiftService.BuildShiftWindow(
                policy,
                attendance.CheckOutTime,
                attendance.EmployeeName);

            if (shift == null)
            {
                return Invalid("Unable to build the employee shift window.");
            }

            if (attendance.CheckOutTime < shift.ShiftEnd ||
                attendance.CheckOutTime > shift.End)
            {
                return Invalid(
                    $"Check-out time {attendance.CheckOutTime:dd-MM-yyyy HH:mm:ss} is outside the allowed shift window.");
            }

            return Valid();
        }

        private static AttendanceValidationResult Valid() =>
            new() { IsValid = true };

        private static AttendanceValidationResult Invalid(string reason) =>
            new() { IsValid = false, Reason = reason };
    }
}
