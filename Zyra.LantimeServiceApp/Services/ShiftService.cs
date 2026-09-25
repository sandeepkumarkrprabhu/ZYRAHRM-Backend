using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Constants;
using Zyra.LantimeServiceApp.Interfaces;
using ZyraHangfireModels.Models;
using ZyraHangfireModels.ServiceModels;

namespace Zyra.LantimeServiceApp.Services
{
    public sealed class ShiftService : IShiftService
    {
        private readonly ILogger<ShiftService> _logger;

        public ShiftService(ILogger<ShiftService> logger)
        {
            _logger = logger;
        }

        public ShiftWindow? BuildShiftWindow(
            EmployeeAttendancePolicy employeeShift,
            DateTime referenceTime,
            string employeeName)
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

            var isOvernightShift = shiftEnd <= shiftStart;
            var shiftEndTime = shiftEnd;

            if (isOvernightShift)
            {
                shiftEnd = shiftEnd.Add(TimeSpan.FromDays(1));
            }

            var shiftDate = referenceTime.Date;

            if (isOvernightShift &&
                referenceTime.TimeOfDay < shiftEndTime)
            {
                shiftDate = shiftDate.AddDays(-1);
            }

            var gracePeriodValue = rules?
                .FirstOrDefault(x =>
                    x.RuleCode == HRMConstants.SHIFT_GRACE_PERIOD_NAME)
                ?.RuleValue;

            var gracePeriod = TimeSpan.Zero;

            if (!string.IsNullOrWhiteSpace(gracePeriodValue) &&
                !TimeSpan.TryParse(gracePeriodValue, out gracePeriod))
            {
                _logger.LogWarning(
                    "Invalid grace period for {EmployeeName}: {GracePeriod}. Using 00:00.",
                    employeeName,
                    gracePeriodValue);

                gracePeriod = TimeSpan.Zero;
            }

            return new ShiftWindow
            {
                Start = shiftDate.Add(shiftStart.Subtract(gracePeriod)),
                End = shiftDate.Add(shiftEnd.Add(gracePeriod)),
                ShiftStart = shiftDate.Add(shiftStart),
                ShiftEnd = shiftDate.Add(shiftEnd),
                IsOvernight = isOvernightShift
            };
        }
    }
}
