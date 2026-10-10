using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models.AssetManagement;

[Table("VendorMaster", Schema = "asset")]
public class VendorMaster
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int VendorId { get; set; }

    [Required]
    [MaxLength(50)]
    public string VendorCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string VendorName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [MaxLength(254)]
    public string? EmailAddress { get; set; }

    [MaxLength(1000)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? TaxRegistrationNumber { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}