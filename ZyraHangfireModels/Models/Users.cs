using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ZyraHangfireModels.Models
{
    public class Users
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string PasswordSalt { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public bool IsLocked { get; set; } = false;

        public bool IsPasswordResetRequired { get; set; } = false;

        [Required]
        [MaxLength(100)]
        public string RoleName { get; set; }

        public DateTime LastLoginDateTime { get; set; }
        public DateTime CreatedDateTime { get; set; }

        [Required]
        public bool Status { get; set; }
    }
}
