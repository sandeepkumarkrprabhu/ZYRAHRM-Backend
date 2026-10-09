using System.ComponentModel.DataAnnotations;

namespace ZYRAHRM.IntegrationApp.DTOs.ClientManagement;

public sealed class ClientWriteRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string ClientCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string ClientName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}

public sealed class ClientResponse
{
    public int ClientId { get; set; }
    public string ClientCode { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class ClientEmployeeAssignmentCreateRequest
{
    [Range(1, int.MaxValue)]
    public int ClientId { get; set; }

    [Range(1, int.MaxValue)]
    public int EmployeeMappingId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ProjectId { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }
}

public sealed class ClientEmployeeAssignmentResponse
{
    public int ClientEmployeeAssignmentId { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int EmployeeMappingId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Remarks { get; set; }
    public string? AssignedBy { get; set; }
    public bool IsActive => !EffectiveTo.HasValue;
}
