
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models
{
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public DateTime Timestamp { get; set; }

        [MaxLength(50)]
        public string userEmail { get; set; }


        [MaxLength(50)]
        public string userName { get; set; }

        
        [MaxLength(150)]
        public string Action { get; set; }

        
        [MaxLength(50)]
        public string Module { get; set; }

        
        [MaxLength(100)]
        public string Details { get; set; }


        [MaxLength(100)]
        public string IPAddress { get; set; }


    }
}
