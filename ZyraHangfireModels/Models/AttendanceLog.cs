using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models
{
    public class AttendanceLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeCode { get; set; }

        [Required]
        public DateTime CheckTime { get; set; }

        [MaxLength(50)]
        public string? DeviceId { get; set; }

        public bool IsProcessed { get; set; } = false;

        public DateTime? ProcessedAt { get; set; }

        [MaxLength(20)]
        public string? AttendanceState { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; } // Success / Failed

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }
    }
}
