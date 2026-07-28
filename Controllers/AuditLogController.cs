using Microsoft.AspNetCore.Mvc;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;
using Microsoft.EntityFrameworkCore;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogController : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(AttendanceDbContext dbContext, ILogger<AuditLogController> logger)
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
                _logger.LogInformation("Fetching Audit logs...");

                var auditLogs = await _dbContext.AuditLog.ToListAsync();

                if (auditLogs == null || !auditLogs.Any())
                {
                    var msgInfo = "No audit logs found.";
                    _logger.LogWarning(msgInfo);
                    return NotFound(msgInfo);
                }

                _logger.LogInformation("Fetched {Count} audit logs.", auditLogs.Count);
                return Ok(auditLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching holidays.");
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - GET single holiday by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _logger.LogInformation("Fetching audit log with ID {Id}...", id);

                var auditLog = await _dbContext.AuditLog
                                              .FirstOrDefaultAsync(e => e.Id == id);

                if (auditLog == null)
                {
                    _logger.LogWarning("Audit log with ID {Id} not found.", id);
                    return NotFound($"Audit log with ID {id} not found.");
                }

                return Ok(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching audit log with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - POST create a new audit log
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AuditLog newAuditLog)
        {
            try
            {
                if (newAuditLog == null)
                {
                    _logger.LogWarning("Create failed: Audit log data is null.");
                    return BadRequest("Audit log data is required.");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create failed: Invalid model state.");
                    return BadRequest(ModelState);
                }

                newAuditLog.Timestamp = DateTime.UtcNow;

                await _dbContext.AuditLog.AddAsync(newAuditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Audit log created successfully with ID {Id}.", newAuditLog.Id);

                return CreatedAtAction(nameof(GetById), new { id = newAuditLog.Id }, newAuditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a new audit log.");
                return StatusCode(500, "Internal server error");
            }
        }

        // EXISTING - PUT update a holiday
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AuditLog updateAuditLog)
        {
            try
            {
                if (updateAuditLog == null)
                {
                    _logger.LogWarning("Update failed: Audit log data is null.");
                    return BadRequest("Audit log data is required.");
                }

                if (id != updateAuditLog.Id)
                {
                    _logger.LogWarning("Update failed: ID mismatch. Route ID: {RouteId}, Body ID: {BodyId}", id, updateAuditLog.Id);
                    return BadRequest("Audit log ID mismatch.");
                }

                var existingAuditLog = await _dbContext.AuditLog    
                                                       .FirstOrDefaultAsync(e => e.Id == id);

                if (existingAuditLog == null)
                {
                    _logger.LogWarning("Audit log with ID {Id} not found.", id);
                    return NotFound($"Audit log with ID {id} not found.");
                }

                existingAuditLog.userEmail = updateAuditLog.userEmail;
                existingAuditLog.userName = updateAuditLog.userName;
                existingAuditLog.Action = updateAuditLog.Action;
                existingAuditLog.Module = updateAuditLog.Module;
                existingAuditLog.Details = updateAuditLog.Details;
                existingAuditLog.IPAddress = updateAuditLog.IPAddress;
                existingAuditLog.Timestamp = DateTime.UtcNow;   // Always set server-side

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Audit log with ID {Id} updated successfully.", id);
                return Ok(existingAuditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating an audit log with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - DELETE a holiday by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting audit log  with ID {Id}...", id);

                var auditLog = await _dbContext.AuditLog
                                               .FirstOrDefaultAsync(e => e.Id == id);

                if (auditLog == null)
                {
                    _logger.LogWarning("Delete failed: Audit log with ID {Id} not found.", id);
                    return NotFound($"Audit log with ID {id} not found.");
                }

                _dbContext.AuditLog.Remove(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Audit log with ID {Id} deleted successfully.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting audit log with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}