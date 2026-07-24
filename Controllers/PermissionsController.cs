using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {

        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(AttendanceDbContext dbContext, ILogger<PermissionsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        //  EXISTING - GET all Permissions
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogInformation("Fetching Permissions...");

                var permissions = await _dbContext.Permissions.ToListAsync();

                if (permissions == null || !permissions.Any())
                {
                    var msgInfo = "No permissions found.";
                    _logger.LogWarning(msgInfo);
                    return NotFound(msgInfo);
                }

                _logger.LogInformation("Fetched {Count} permissions.", permissions.Count);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching permissions.");
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - GET single permission by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _logger.LogInformation("Fetching permission with ID {Id}...", id);

                var permission = await _dbContext.Permissions
                                                    .FirstOrDefaultAsync(e => e.Id == id);

                if (permission == null)
                {
                    _logger.LogWarning("Permission with ID {Id} not found.", id);
                    return NotFound($"Permission with ID {id} not found.");
                }

                return Ok(permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching permission with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - POST create a new permission
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Permissions newPermission)
        {
            try
            {
                if (newPermission == null)
                {
                    _logger.LogWarning("Create failed: Permission data is null.");
                    return BadRequest("Permission data is required.");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create failed: Invalid model state.");
                    return BadRequest(ModelState);
                }

                await _dbContext.Permissions.AddAsync(newPermission);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Permission created successfully with ID {Id}.", newPermission.Id);

                return CreatedAtAction(nameof(GetById), new { id = newPermission.Id }, newPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a new permission.");
                return StatusCode(500, "Internal server error");
            }
        }

        // EXISTING - PUT update a permission
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Permissions updatePermission)
        {
            try
            {
                if (updatePermission == null)
                {
                    _logger.LogWarning("Update failed: Permission data is null.");
                    return BadRequest("Permission data is required.");
                }

                if (id != updatePermission.Id)
                {
                    _logger.LogWarning("Update failed: ID mismatch. Route ID: {RouteId}, Body ID: {BodyId}", id, updatePermission.Id);
                    return BadRequest("Permission ID mismatch.");
                }

                var existingPermission = await _dbContext.Permissions
                                                        .FirstOrDefaultAsync(e => e.Id == id);

                if (existingPermission == null)
                {
                    _logger.LogWarning("Permission with ID {Id} not found.", id);
                    return NotFound($"Permission with ID {id} not found.");
                }

                existingPermission.Id = updatePermission.Id;
                existingPermission.PermissionName = updatePermission.PermissionName;
                existingPermission.Description = updatePermission.Description;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Permission with ID {Id} updated successfully.", id);
                return Ok(existingPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating permission with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - DELETE a permission by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting permission with ID {Id}...", id);

                var permission = await _dbContext.Permissions
                                                 .FirstOrDefaultAsync(e => e.Id == id);

                if (permission == null)
                {
                    _logger.LogWarning("Delete failed: Permission with ID {Id} not found.", id);
                    return NotFound($"Permission with ID {id} not found.");
                }

                _dbContext.Permissions.Remove(permission);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Permission with ID {Id} deleted successfully.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting permission with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
