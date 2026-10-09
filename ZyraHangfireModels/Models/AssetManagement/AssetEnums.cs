namespace ZyraHangfireModels.Models.AssetManagement;

/// <summary>
/// Indicates whether an asset is owned by the company or rented from a vendor.
/// </summary>
public enum AssetOwnershipType
{
    Owned = 1,
    Rented = 2,
    ClientOwned = 3
}

/// <summary>
/// Current operational state of an asset.
/// </summary>
public enum AssetStatus
{
    Available = 1,
    Assigned = 2,
    UnderMaintenance = 3,
    Retired = 4,
    Lost = 5
}
