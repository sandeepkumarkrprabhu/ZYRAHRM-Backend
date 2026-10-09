using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models.AssetManagement;

/// <summary>
/// Keeps a historical record of each asset status transition.
/// </summary>
[Table("AssetStatusHistory", Schema = "asset")]
public class AssetStatusHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AssetStatusHistoryId { get; set; }

    public int AssetId { get; set; }

    public AssetStatus? PreviousStatus { get; set; }

    public AssetStatus NewStatus { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? ChangedBy { get; set; }

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    public Asset? Asset { get; set; }
}
