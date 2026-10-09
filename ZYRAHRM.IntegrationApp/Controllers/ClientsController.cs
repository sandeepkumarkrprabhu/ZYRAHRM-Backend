using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZYRAHRM.IntegrationApp.DTOs.ClientManagement;
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
    [ProducesResponseType(typeof(IReadOnlyList<ClientResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClientResponse>>> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ClientMasters.AsNoTracking();
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        var clients = await query.OrderBy(x => x.ClientName)
            .Select(x => new ClientResponse
            {
                ClientId = x.ClientId,
                ClientCode = x.ClientCode,
                ClientName = x.ClientName,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .ToListAsync(cancellationToken);

        return Ok(clients);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientResponse>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.ClientMasters.AsNoTracking()
            .Where(x => x.ClientId == id)
            .Select(x => new ClientResponse
            {
                ClientId = x.ClientId,
                ClientCode = x.ClientCode,
                ClientName = x.ClientName,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .SingleOrDefaultAsync(cancellationToken);

        return client is null ? NotFound() : Ok(client);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientResponse>> Create(
        [FromBody] ClientWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.ClientCode.Trim();
        var name = request.ClientName.Trim();

        if (await _dbContext.ClientMasters.AnyAsync(x => x.ClientCode == code, cancellationToken))
            return Conflict(new { message = "A client with this code already exists." });

        var client = new ClientMaster
        {
            ClientCode = code,
            ClientName = name,
            Description = Normalize(request.Description),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentActor()
        };

        _dbContext.ClientMasters.Add(client);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The client could not be created. Verify that the client code is unique." });
        }

        var response = ToResponse(client);
        return CreatedAtAction(nameof(GetById), new { id = client.ClientId }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientResponse>> Update(
        int id,
        [FromBody] ClientWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.ClientMasters
            .SingleOrDefaultAsync(x => x.ClientId == id, cancellationToken);
        if (client is null)
            return NotFound();

        var code = request.ClientCode.Trim();
        if (await _dbContext.ClientMasters.AnyAsync(
                x => x.ClientId != id && x.ClientCode == code, cancellationToken))
        {
            return Conflict(new { message = "A client with this code already exists." });
        }

        client.ClientCode = code;
        client.ClientName = request.ClientName.Trim();
        client.Description = Normalize(request.Description);
        client.UpdatedAt = DateTime.UtcNow;
        client.UpdatedBy = CurrentActor();

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The client could not be updated. Verify that the client code is unique." });
        }

        return Ok(ToResponse(client));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        int id,
        CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.ClientMasters
            .SingleOrDefaultAsync(x => x.ClientId == id, cancellationToken);
        if (client is null)
            return NotFound();

        if (!client.IsActive)
            return NoContent();

        // Keep the client record and assignment/asset history; deactivation is a soft delete.
        client.IsActive = false;
        client.UpdatedAt = DateTime.UtcNow;
        client.UpdatedBy = CurrentActor();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private string? CurrentActor() => User.Identity?.Name;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ClientResponse ToResponse(ClientMaster client) => new()
    {
        ClientId = client.ClientId,
        ClientCode = client.ClientCode,
        ClientName = client.ClientName,
        Description = client.Description,
        IsActive = client.IsActive,
        CreatedAt = client.CreatedAt,
        CreatedBy = client.CreatedBy,
        UpdatedAt = client.UpdatedAt,
        UpdatedBy = client.UpdatedBy
    };
}