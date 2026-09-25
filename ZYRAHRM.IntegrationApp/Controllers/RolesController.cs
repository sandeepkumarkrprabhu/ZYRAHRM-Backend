
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<RolesController> _logger;

        public RolesController(AttendanceDbContext dbContext, ILogger<RolesController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }



        //  EXISTING - GET all holidays
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogInformation("Fetching Roles...");

                var roles = await _dbContext.Roles.ToListAsync();

                if (roles == null || !roles.Any())
                {
                    var msgInfo = "No roles found.";
                    _logger.LogWarning(msgInfo);
                    return NotFound(msgInfo);
                }

                _logger.LogInformation("Fetched {Count} roles.", roles.Count);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching roles.");
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - GET single role by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _logger.LogInformation("Fetching role with ID {Id}...", id);

                var role = await _dbContext.Roles
                                            .FirstOrDefaultAsync(e => e.Id == id);

                if (role == null)
                {
                    _logger.LogWarning("Role with ID {Id} not found.", id);
                    return NotFound($"Role with ID {id} not found.");
                }

                return Ok(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching role with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - POST create a new role
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Roles newRole)
        {
            try
            {
                if (newRole == null)
                {
                    _logger.LogWarning("Create failed: Role data is null.");
                    return BadRequest("Role data is required.");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create failed: Invalid model state.");
                    return BadRequest(ModelState);
                }

                newRole.CreatedDateTime = DateTime.UtcNow;
                newRole.UpdatedDateTime = DateTime.UtcNow;

                await _dbContext.Roles.AddAsync(newRole);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Role created successfully with ID {Id}.", newRole.Id);

                return CreatedAtAction(nameof(GetById), new { id = newRole.Id }, newRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a new role.");
                return StatusCode(500, "Internal server error");
            }
        }

        // EXISTING - PUT update a role
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Roles updateRole)
        {
            try
            {
                if (updateRole == null)
                {
                    _logger.LogWarning("Update failed: Role data is null.");
                    return BadRequest("Role data is required.");
                }

                if (id != updateRole.Id)
                {
                    _logger.LogWarning("Update failed: ID mismatch. Route ID: {RouteId}, Body ID: {BodyId}", id, updateRole.Id);
                    return BadRequest("Role ID mismatch.");
                }

                var existingRole = await _dbContext.Roles
                                                    .FirstOrDefaultAsync(e => e.Id == id);

                if (existingRole == null)
                {
                    _logger.LogWarning("Role with ID {Id} not found.", id);
                    return NotFound($"Role with ID {id} not found.");
                }

                existingRole.Id = updateRole.Id;
                existingRole.RoleName = updateRole.RoleName;
                existingRole.Description = updateRole.Description               ;
                existingRole.IsActive = updateRole.IsActive;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Role with ID {Id} updated successfully.", id);
                return Ok(existingRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating role with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - DELETE a role by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting role with ID {Id}...", id);

                var role = await _dbContext.Roles
                                           .FirstOrDefaultAsync(e => e.Id == id);

                if (role == null)
                {
                    _logger.LogWarning("Delete failed: Role with ID {Id} not found.", id);
                    return NotFound($"Role with ID {id} not found.");
                }

                _dbContext.Roles.Remove(role);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Role with ID {Id} deleted successfully.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting role with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
