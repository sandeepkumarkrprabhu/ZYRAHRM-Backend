using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRAHRM.IntegrationApp.Controllers;

[ApiController]
[Authorize]
[Route("api/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly AttendanceDbContext _dbContext;

    public ClientsController(AttendanceDbContext dbContext) => _dbContext = dbContext;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClientMaster>>> GetAll(
        [FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ClientMasters.AsNoTracking();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        var clients = await query.OrderBy(x => x.ClientName)
            .Select(x => new ClientMaster
            {
                ClientId = x.ClientId, ClientCode = x.ClientCode, ClientName = x.ClientName,
                Description = x.Description, IsActive = x.IsActive, CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy, UpdatedAt = x.UpdatedAt, UpdatedBy = x.UpdatedBy
            }).ToListAsync(cancellationToken);
        return Ok(clients);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientMaster>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.ClientMasters.AsNoTracking()
            .Where(x => x.ClientId == id)
            .Select(x => new ClientMaster
            {
                ClientId = x.ClientId, ClientCode = x.ClientCode, ClientName = x.ClientName,
                Description = x.Description, IsActive = x.IsActive, CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy, UpdatedAt = x.UpdatedAt, UpdatedBy = x.UpdatedBy
            }).SingleOrDefaultAsync(cancellationToken);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<ClientMaster>> Create([FromBody] ClientMaster request,
        CancellationToken cancellationToken = default)
    {
        var code = request.ClientCode?.Trim();
        var name = request.ClientName?.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "ClientCode and ClientName are required." });
        if (await _dbContext.ClientMasters.AnyAsync(x => x.ClientCode == code, cancellationToken))
            return Conflict(new { message = "A client with this code already exists." });

        var client = new ClientMaster
        {
            ClientCode = code, ClientName = name, Description = Normalize(request.Description),
            IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = User.Identity?.Name
        };
        _dbContext.ClientMasters.Add(client);
        try { await _dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Conflict(new { message = "Client could not be created; verify the client code is unique." }); }
        return CreatedAtAction(nameof(GetById), new { id = client.ClientId }, client);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClientMaster>> Update(int id, [FromBody] ClientMaster request,
        CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.ClientMasters.SingleOrDefaultAsync(x => x.ClientId == id, cancellationToken);
        if (client is null) return NotFound();
        var code = request.ClientCode?.Trim();
        var name = request.ClientName?.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "ClientCode and ClientName are required." });
        if (await _dbContext.ClientMasters.AnyAsync(x => x.ClientId != id && x.ClientCode == code, cancellationToken))
            return Conflict(new { message = "A client with this code already exists." });

        client.ClientCode = code; client.ClientName = name;
        client.Description = Normalize(request.Description); client.UpdatedAt = DateTime.UtcNow;
        client.UpdatedBy = User.Identity?.Name;
        try { await _dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Conflict(new { message = "Client could not be updated; verify the client code is unique." }); }
        return Ok(client);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.ClientMasters.SingleOrDefaultAsync(x => x.ClientId == id, cancellationToken);
        if (client is null) return NotFound();
        if (!client.IsActive) return NoContent();
        client.IsActive = false; client.UpdatedAt = DateTime.UtcNow; client.UpdatedBy = User.Identity?.Name;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}