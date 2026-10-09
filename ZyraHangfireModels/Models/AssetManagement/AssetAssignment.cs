using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ZyraHangfireModels.Models;

namespace ZyraHangfireModels.Models.AssetManagement;

/// <summary>
/// Records an asset assignment. Returned assignments remain as history.
/// </summary>
[Table("AssetAssignments", Schema = "asset")]
public class AssetAssignment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AssetAssignmentId { get; set; }

    public int AssetId { get; set; }

    /// <summary>
    /// References the existing EmployeeMapping.Id; no employee table is duplicated.
    /// </summary>
    public int EmployeeMappingId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReturnedAt { get; set; }

    [MaxLength(1000)]
    public string? AssignmentRemarks { get; set; }

    [MaxLength(1000)]
    public string? ReturnRemarks { get; set; }

    [MaxLength(100)]
    public string? AssignedBy { get; set; }

    [MaxLength(100)]
    public string? ReturnedBy { get; set; }

    public Asset? Asset { get; set; }

    public EmployeeMapping? EmployeeMapping { get; set; }
}
