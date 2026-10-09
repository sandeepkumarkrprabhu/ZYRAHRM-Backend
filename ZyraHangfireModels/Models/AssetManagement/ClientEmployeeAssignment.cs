using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ZyraHangfireModels.Models;

namespace ZyraHangfireModels.Models.AssetManagement;

/// <summary>
/// Tracks an employee's assignment to a client and optionally a project.
/// </summary>
[Table("ClientEmployeeAssignments", Schema = "asset")]
public class ClientEmployeeAssignment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ClientEmployeeAssignmentId { get; set; }

    public int ClientId { get; set; }

    /// <summary>References the existing EmployeeMapping.Id; does not duplicate employee data.</summary>
    public int EmployeeMappingId { get; set; }

    /// <summary>Optional project identifier; no project FK is configured until the project master is identified.</summary>
    public int? ProjectId { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveTo { get; set; }

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    [MaxLength(100)]
    public string? AssignedBy { get; set; }

    public ClientMaster? Client { get; set; }

    public EmployeeMapping? EmployeeMapping { get; set; }
}
