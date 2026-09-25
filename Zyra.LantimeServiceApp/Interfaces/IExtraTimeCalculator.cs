using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IExtraTimeCalculator
    {
        ExtraTimeInterval? Calculate(
            DateTime shiftEnd,
            IEnumerable<DateTime> punches,
            DateTime evaluationTime,
            int maximumOvertimeMinutes);
    }
}