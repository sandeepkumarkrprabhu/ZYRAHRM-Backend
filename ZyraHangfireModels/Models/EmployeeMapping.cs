using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZyraHangfireModels.Models
{
    public class EmployeeMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BiometricUserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string HRMEmployeeCode { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeName { get; set; }

        // 🔹 Common Fields
        public bool IsActive { get; set; } = true;

        public bool IsExcludeFromBiometric { get; set; } = false;

        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string CreatedUser { get; set; }

        public DateTime UpdatedDateTime { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string UpdatedUser { get; set; }

        public DateTime LatestCheckoutFromBiometric { get; set; }

        public DateTime LastCheckoutFinal { get; set; }

        public bool IsCheckoutFinalOverriddenByHR { get; set; }
    }
}
