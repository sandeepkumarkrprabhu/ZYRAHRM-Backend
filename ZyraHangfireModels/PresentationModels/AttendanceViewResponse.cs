
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.PresentationModels
{
    public class AttendanceViewResponse
    {
        public long Id { get; set; }

        public string EmployeeCode { get; set; }

        public DateTime CheckTime { get; set; }

        public string? DeviceId { get; set; }

        public bool IsProcessed { get; set; } = false;

        public DateTime? ProcessedAt { get; set; }

        public string? AttendanceState { get; set; }

        public string? Status { get; set; }

        public string? ErrorMessage { get; set; }

        public string EmployeeName { get; set; }

        public string PunchType { get; set; }
    }
}
