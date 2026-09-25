
namespace ZyraHangfireModels.ServiceModels
{
    public sealed class ShiftWindow
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public DateTime ShiftStart { get; set; }

        public DateTime ShiftEnd { get; set; }

        public bool IsOvernight { get; set; }
    }
}
