using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Services
{
    public sealed class ExtraTimeCalculator : IExtraTimeCalculator
    {
        public ExtraTimeInterval? Calculate(
            DateTime shiftEnd,
            IEnumerable<DateTime> punches,
            DateTime evaluationTime,
            int maximumOvertimeMinutes)
        {
            if (maximumOvertimeMinutes <= 0)
                return null;

            var latestPunch = punches
                .Where(x => x > shiftEnd)
                .Where(x => x <= shiftEnd.AddMinutes(maximumOvertimeMinutes))
                .Where(x => x <= evaluationTime)
                .Distinct()
                .OrderByDescending(x => x)
                .FirstOrDefault();

            if (latestPunch == default)
                return null;

            var duration = (int)(latestPunch - shiftEnd).TotalMinutes;

            return duration > 0
                ? new ExtraTimeInterval
                {
                    CheckInTime = shiftEnd,
                    CheckOutTime = latestPunch,
                    DurationMinutes = duration
                }
                : null;
        }
    }
}