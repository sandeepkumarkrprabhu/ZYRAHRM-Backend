using System.ComponentModel.DataAnnotations;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRAHRM.IntegrationApp.DTOs.AssetManagement;

public sealed class AssetCategoryWriteRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public sealed class AssetCategoryResponse
{
    public int AssetCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class VendorWriteRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string VendorCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string VendorName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? ContactPerson { get; set; }

    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(254)]
    public string? EmailAddress { get; set; }

    [StringLength(1000)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? TaxRegistrationNumber { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public sealed class VendorResponse
{
    public int VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Address { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class AssetWriteRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string AssetCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string AssetName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int AssetCategoryId { get; set; }

    [StringLength(100)]
    public string? Manufacturer { get; set; }

    [StringLength(100)]
    public string? ModelNumber { get; set; }

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [EnumDataType(typeof(AssetOwnershipType))]
    public AssetOwnershipType OwnershipType { get; set; } = AssetOwnershipType.Owned;

    /// <summary>Required when OwnershipType is ClientOwned; null for company-owned/rented assets.</summary>
    public int? ClientId { get; set; }

    /// <summary>Supplier for a purchase or rental. Required for rented assets.</summary>
    public int? VendorId { get; set; }

    [StringLength(100)]
    public string? ProcurementReference { get; set; }

    public DateTime? RentalStartDate { get; set; }

    public DateTime? RentalEndDate { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? RentalCost { get; set; }

    [StringLength(100)]
    public string? RentalCostFrequency { get; set; }

    public DateTime? PurchaseDate { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? PurchaseCost { get; set; }

    public DateTime? WarrantyExpiryDate { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    [StringLength(256)]
    public string? DeviceAdminAccountName { get; set; }

    /// <summary>Identifier/reference for a credential stored in a secret vault; never send a password here.</summary>
    [StringLength(500)]
    public string? DeviceAdminCredentialSecretReference { get; set; }

    [StringLength(500)]
    public string? DeviceAdminAccountNotes { get; set; }
}

public sealed class AssetResponse
{
    public int AssetId { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public int? VendorId { get; set; }
    public string? VendorCode { get; set; }
    public string? VendorName { get; set; }
    public string? ProcurementReference { get; set; }
    public DateTime? RentalStartDate { get; set; }
    public DateTime? RentalEndDate { get; set; }
    public decimal? RentalCost { get; set; }
    public string? RentalCostFrequency { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int AssetCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public AssetOwnershipType OwnershipType { get; set; }
    public AssetStatus Status { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string? Location { get; set; }
    public string? Remarks { get; set; }
    public string? DeviceAdminAccountName { get; set; }
    public bool HasDeviceAdminCredentialReference { get; set; }
    public string? DeviceAdminAccountNotes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
