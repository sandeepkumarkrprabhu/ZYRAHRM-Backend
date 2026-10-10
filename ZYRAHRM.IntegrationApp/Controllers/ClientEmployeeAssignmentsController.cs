using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZYRAHRM.IntegrationApp.DTOs.ClientManagement;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRAHRM.IntegrationApp.Controllers;

[ApiController]
[Route("api/client-employee-assignments")]
public sealed class ClientEmployeeAssignmentsController : ControllerBase
{
    private readonly AttendanceDbContext _dbContext;

    public ClientEmployeeAssignmentsController(AttendanceDbContext dbContext) => _dbContext = dbContext;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClientEmployeeAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClientEmployeeAssignmentResponse>>> GetAll(
        [FromQuery] int? clientId = null,
        [FromQuery] int? employeeMappingId = null,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (clientId is <= 0 || employeeMappingId is <= 0)
            return BadRequest(new { message = "ClientId and EmployeeMappingId filters must be positive integers." });

        var query = _dbContext.ClientEmployeeAssignments.AsNoTracking();
        if (clientId.HasValue)
            query = query.Where(x => x.ClientId == clientId.Value);
        if (employeeMappingId.HasValue)
            query = query.Where(x => x.EmployeeMappingId == employeeMappingId.Value);
        if (activeOnly)
            query = query.Where(x => x.EffectiveTo == null);

        var result = await query.OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new ClientEmployeeAssignmentResponse
            {
                ClientEmployeeAssignmentId = x.ClientEmployeeAssignmentId,
                ClientId = x.ClientId,
                ClientName = x.Client!.ClientName,
                EmployeeMappingId = x.EmployeeMappingId,
                EmployeeName = x.EmployeeMapping!.EmployeeName,
                ProjectId = x.ProjectId,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo,
                Remarks = x.Remarks,
                AssignedBy = x.AssignedBy
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClientEmployeeAssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientEmployeeAssignmentResponse>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.ClientEmployeeAssignments.AsNoTracking()
            .Where(x => x.ClientEmployeeAssignmentId == id)
            .Select(x => new ClientEmployeeAssignmentResponse
            {
                ClientEmployeeAssignmentId = x.ClientEmployeeAssignmentId,
                ClientId = x.ClientId,
                ClientName = x.Client!.ClientName,
                EmployeeMappingId = x.EmployeeMappingId,
                EmployeeName = x.EmployeeMapping!.EmployeeName,
                ProjectId = x.ProjectId,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo,
                Remarks = x.Remarks,
                AssignedBy = x.AssignedBy
            })
            .SingleOrDefaultAsync(cancellationToken);

        return assignment is null ? NotFound() : Ok(assignment);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClientEmployeeAssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientEmployeeAssignmentResponse>> Create(
        [FromBody] ClientEmployeeAssignmentCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var clientIsActive = await _dbContext.ClientMasters.AnyAsync(
            x => x.ClientId == request.ClientId && x.IsActive, cancellationToken);
        if (!clientIsActive)
            return BadRequest(new { message = "ClientId must reference an existing active client." });

        var employee = await _dbContext.EmployeeMappings.AsNoTracking()
            .Where(x => x.Id == request.EmployeeMappingId && x.IsActive)
            .Select(x => new { x.Id, x.EmployeeName })
            .SingleOrDefaultAsync(cancellationToken);
        if (employee is null)
            return BadRequest(new { message = "EmployeeMappingId must reference an existing active employee." });

        var effectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow;
        if (effectiveFrom.Kind == DateTimeKind.Unspecified)
            effectiveFrom = DateTime.SpecifyKind(effectiveFrom, DateTimeKind.Utc);
        else
            effectiveFrom = effectiveFrom.ToUniversalTime();

        var alreadyAssigned = await _dbContext.ClientEmployeeAssignments.AnyAsync(
            x => x.ClientId == request.ClientId &&
                 x.EmployeeMappingId == request.EmployeeMappingId &&
                 x.EffectiveTo == null,
            cancellationToken);
        if (alreadyAssigned)
            return Conflict(new { message = "This employee already has an open allocation for this client. End it before creating another." });

        var assignment = new ClientEmployeeAssignment
        {
            ClientId = request.ClientId,
            EmployeeMappingId = request.EmployeeMappingId,
            ProjectId = request.ProjectId,
            EffectiveFrom = effectiveFrom,
            Remarks = Normalize(request.Remarks),
            AssignedBy = CurrentActor()
        };

        _dbContext.ClientEmployeeAssignments.Add(assignment);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The allocation could not be saved. Verify the client/employee references and ensure no duplicate open allocation exists." });
        }

        var response = new ClientEmployeeAssignmentResponse
        {
            ClientEmployeeAssignmentId = assignment.ClientEmployeeAssignmentId,
            ClientId = assignment.ClientId,
            ClientName = await _dbContext.ClientMasters
                .Where(x => x.ClientId == assignment.ClientId)
                .Select(x => x.ClientName).SingleAsync(cancellationToken),
            EmployeeMappingId = assignment.EmployeeMappingId,
            EmployeeName = employee.EmployeeName,
            ProjectId = assignment.ProjectId,
            EffectiveFrom = assignment.EffectiveFrom,
            EffectiveTo = assignment.EffectiveTo,
            Remarks = assignment.Remarks,
            AssignedBy = assignment.AssignedBy
        };

        return CreatedAtAction(nameof(GetById), new { id = assignment.ClientEmployeeAssignmentId }, response);
    }

    [HttpPost("{id:int}/end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> End(
        int id,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.ClientEmployeeAssignments
            .SingleOrDefaultAsync(x => x.ClientEmployeeAssignmentId == id, cancellationToken);
        if (assignment is null)
            return NotFound();

        if (assignment.EffectiveTo.HasValue)
            return Conflict(new { message = "This client allocation has already ended." });

        var endedAt = DateTime.UtcNow;
        if (endedAt < assignment.EffectiveFrom)
            return Conflict(new { message = "This allocation has a future effective date and cannot be ended yet." });

        assignment.EffectiveTo = endedAt;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private string? CurrentActor() => User.Identity?.Name;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}