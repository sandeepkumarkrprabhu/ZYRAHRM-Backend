using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models
{
    public class Modules
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ModuleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ModuleCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ModuleName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
