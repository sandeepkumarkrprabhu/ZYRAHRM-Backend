
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ZyraHangfireModels.Models
{
    public class AttendancePolicyRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }


        public int? AttendancePolicyId { get; set; }

        [MaxLength(50)]
        public string? RuleCode { get; set; }

        [MaxLength(50)]
        public string? RuleValue { get; set; }

        // Navigation property
        [ForeignKey(nameof(AttendancePolicyId))]
        [JsonIgnore]
        public AttendancePolicyMaster? AttendancePolicy { get; set; }
    }
}
