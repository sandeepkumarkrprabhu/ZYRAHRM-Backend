
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models
{
    public class EmployeeAttendancePolicy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        
        public int EmployeeId { get; set; }
        
        public int AttendancePolicyId { get; set; }
        
        public DateTime EffectiveFrom { get; set; }
        
        public DateTime EffectiveTo { get; set; }

        public DateTime AutoCheckInTime { get; set; }

        public DateTime AutoCheckOutTime { get; set; }
 
        public bool IsEnabled { get; set; }
        
        public DateTime CreatedOn { get; set; }
        
        public DateTime UpdatedOn { get; set; }

        public AttendancePolicyMaster? AttendancePolicy { get; set; } = null;

        public EmployeeMapping? EmployeeMapping { get; set; } = null;

    }
}
