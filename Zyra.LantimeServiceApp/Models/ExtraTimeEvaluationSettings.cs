namespace Zyra.LantimeServiceApp.Models
{
    public sealed class ExtraTimeEvaluationSettings
    {
        public int DayShiftEvaluationDelayMinutes { get; set; } = 60;
        public int OvernightShiftEvaluationDelayMinutes { get; set; } = 120;
        public int MaximumOvertimeMinutes { get; set; } = 240;
    }
}
