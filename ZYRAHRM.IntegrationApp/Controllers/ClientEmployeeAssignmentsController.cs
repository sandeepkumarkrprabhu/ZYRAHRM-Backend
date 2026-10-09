using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRAHRM.IntegrationApp.Controllers;

[ApiController]
[Authorize]
[Route("api/client-employee-assignments")]
public sealed class ClientEmployeeAssignmentsController : ControllerBase
{
    private readonly AttendanceDbContext _dbContext;

    public ClientEmployeeAssignmentsController(AttendanceDbContext dbContext) => _dbContext = dbContext;

    public sealed class CreateRequest
    {
        public int ClientId { get; set; }
        public int EmployeeMappingId { get; set; }
        public int? ProjectId { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public string? Remarks { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? clientId = null,
        [FromQuery] int? employeeMappingId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ClientEmployeeAssignments.AsNoTracking().AsQueryable();
        if (clientId.HasValue) query = query.Where(x => x.ClientId == clientId.Value);
        if (employeeMappingId.HasValue) query = query.Where(x => x.EmployeeMappingId == employeeMappingId.Value);
        var result = await query.OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new
            {
                x.ClientEmployeeAssignmentId, x.ClientId, ClientName = x.Client!.ClientName,
                x.EmployeeMappingId, EmployeeName = x.EmployeeMapping!.EmployeeName,
                x.ProjectId, x.EffectiveFrom, x.EffectiveTo, x.Remarks, x.AssignedBy
            }).ToListAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ClientId <= 0 || request.EmployeeMappingId <= 0)
            return BadRequest(new { message = "ClientId and EmployeeMappingId are required." });

        var clientIsActive = await _dbContext.ClientMasters.AnyAsync(
            x => x.ClientId == request.ClientId && x.IsActive, cancellationToken);
        if (!clientIsActive)
            return BadRequest(new { message = "ClientId must reference an active client." });

        var employeeIsActive = await _dbContext.EmployeeMappings.AnyAsync(
            x => x.Id == request.EmployeeMappingId && x.IsActive, cancellationToken);
        if (!employeeIsActive)
            return BadRequest(new { message = "EmployeeMappingId must reference an active employee." });

        var alreadyAssigned = await _dbContext.ClientEmployeeAssignments.AnyAsync(
            x => x.ClientId == request.ClientId &&
                 x.EmployeeMappingId == request.EmployeeMappingId && x.EffectiveTo == null,
            cancellationToken);
        if (alreadyAssigned)
            return Conflict(new { message = "This employee already has an active allocation for this client." });

        var assignment = new ClientEmployeeAssignment
        {
            ClientId = request.ClientId,
            EmployeeMappingId = request.EmployeeMappingId,
            ProjectId = request.ProjectId,
            EffectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            AssignedBy = User.Identity?.Name
        };
        _dbContext.ClientEmployeeAssignments.Add(assignment);
        try { await _dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Conflict(new { message = "The client allocation could not be saved. Verify the client and employee references." }); }
        return CreatedAtAction(nameof(GetAll), new { clientId = assignment.ClientId, employeeMappingId = assignment.EmployeeMappingId }, assignment);
    }

    [HttpPost("{id:int}/end")]
    public async Task<IActionResult> End(int id, CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.ClientEmployeeAssignments
            .SingleOrDefaultAsync(x => x.ClientEmployeeAssignmentId == id, cancellationToken);
        if (assignment is null) return NotFound();
        if (assignment.EffectiveTo.HasValue) return Conflict(new { message = "This client allocation has already ended." });
        assignment.EffectiveTo = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}