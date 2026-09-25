
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models
{
    public class NavigationMenus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MenuId { get; set; }

        [Required]
        [MaxLength(10)]
        public string MenuKey { get; set; }

        [Required]
        [MaxLength(150)]
        public string MenuLabel { get; set; }

        [MaxLength(100)]
        public string IconName { get; set; }

        public int DisplayOrder { get; set; }

        [MaxLength(150)]
        public string RequiredPermissionCode { get; set; }

        [MaxLength(150)]
        public string Description { get; set; }
    }
}
