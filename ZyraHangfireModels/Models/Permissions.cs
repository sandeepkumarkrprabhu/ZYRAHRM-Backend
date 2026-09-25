using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ZyraHangfireModels.Models
{
    public class Permissions
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string PermissionCode { get; set; }

        [MaxLength(50)]
        public string PermissionName { get; set; }

        [MaxLength(50)]
        public string Category { get; set; }

        [MaxLength(150)]
        public string Description { get; set; }
    }
}
