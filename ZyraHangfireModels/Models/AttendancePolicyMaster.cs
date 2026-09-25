
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models
{
    public class AttendancePolicyMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string PolicyName { get; set; }

        public string? PolicyType { get; set; }

        public int Priority { get; set; } = 0;

        public bool IsEnable { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime UpdatedOn { get; set; } = DateTime.Now;

        public bool? IsDefault { get; set; } = false;

        // One Policy can have multiple Rules
        public ICollection<AttendancePolicyRule>? Rules { get; set; }
            = new List<AttendancePolicyRule>();

        public ICollection<EmployeeMapping>? EmployeeMappings { get; set; }
        = new List<EmployeeMapping>();

    }
}
