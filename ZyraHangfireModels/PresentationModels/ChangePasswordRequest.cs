using System.ComponentModel.DataAnnotations;

namespace ZyraHangfireModels.PresentationModels
{
    public class ChangePasswordRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
