using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models.AssetManagement;

/// <summary>
/// Represents a physical company asset or an asset rented by the company.
/// </summary>
[Table("Assets", Schema = "asset")]
public class Asset
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AssetId { get; set; }

    [Required]
    [MaxLength(50)]
    public string AssetCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string AssetName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int AssetCategoryId { get; set; }

    /// <summary>Required for client-owned devices; null for company-owned or rented devices.</summary>
    public int? ClientId { get; set; }

    /// <summary>Vendor that sold or currently rents this asset to the company.</summary>
    public int? VendorId { get; set; }

    /// <summary>Purchase order, invoice, rental agreement or supplier reference.</summary>
    [MaxLength(100)]
    public string? ProcurementReference { get; set; }

    /// <summary>Rental commencement date, when applicable.</summary>
    public DateTime? RentalStartDate { get; set; }

    /// <summary>Expected rental return/contract end date, when applicable.</summary>
    public DateTime? RentalEndDate { get; set; }

    /// <summary>Recurring rental charge, if the vendor contract has one.</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? RentalCost { get; set; }

    [MaxLength(100)]
    public string? RentalCostFrequency { get; set; }

    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    public string? ModelNumber { get; set; }

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    public AssetOwnershipType OwnershipType { get; set; } = AssetOwnershipType.Owned;

    public AssetStatus Status { get; set; } = AssetStatus.Available;

    public DateTime? PurchaseDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? PurchaseCost { get; set; }

    public DateTime? WarrantyExpiryDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    /// <summary>Microsoft account or administrator username configured on this device.</summary>
    [MaxLength(256)]
    public string? DeviceAdminAccountName { get; set; }

    /// <summary>Reference to the credential stored in an approved secret vault. Never store the password here.</summary>
    [MaxLength(500)]
    public string? DeviceAdminCredentialSecretReference { get; set; }

    [MaxLength(500)]
    public string? DeviceAdminAccountNotes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public AssetCategory? Category { get; set; }

    public ClientMaster? Client { get; set; }

    public VendorMaster? Vendor { get; set; }

    public ICollection<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();

    public ICollection<AssetStatusHistory> StatusHistory { get; set; } = new List<AssetStatusHistory>();
}
