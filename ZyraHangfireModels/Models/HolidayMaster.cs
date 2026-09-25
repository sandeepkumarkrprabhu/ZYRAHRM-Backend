
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models
{
    public class HolidayMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string HolidayCode { get; set; }

        public string HolidayName { get; set; }

        public string HolidayType { get; set; }

        public string Description { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
