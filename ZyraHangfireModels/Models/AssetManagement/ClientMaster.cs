using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZyraHangfireModels.Models.AssetManagement;

/// <summary>External client/customer for whom employees may work.</summary>
[Table("ClientMaster", Schema = "asset")]
public class ClientMaster
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ClientId { get; set; }

    [Required, MaxLength(50)]
    public string ClientCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string ClientName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public ICollection<ClientEmployeeAssignment> EmployeeAssignments { get; set; } = new List<ClientEmployeeAssignment>();
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}