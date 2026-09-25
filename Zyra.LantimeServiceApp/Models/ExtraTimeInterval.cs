namespace Zyra.LantimeServiceApp.Models
{
    public sealed class ExtraTimeInterval
    {
        public DateTime CheckInTime { get; init; }
        public DateTime CheckOutTime { get; init; }
        public int DurationMinutes { get; init; }
    }
}