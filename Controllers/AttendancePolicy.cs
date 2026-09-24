using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendancePolicy : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<AttendancePolicy> _logger;

        public AttendancePolicy(AttendanceDbContext dbContext, ILogger<AttendancePolicy> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        //  EXISTING - GET all Attendance Policies
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogInformation("Fetching Attendance Policy mappings...");

                var attendancePolicies = await _dbContext.AttendancePolicyMasters.Include(p => p.Rules).ToListAsync();

                if (attendancePolicies == null || !attendancePolicies.Any())
                {
                    var msgInfo = "No attendance policies found.";
                    _logger.LogWarning(msgInfo);
                    return NotFound(msgInfo);
                }

                _logger.LogInformation("Fetched {Count} attendance Policies.", attendancePolicies.Count);
                return Ok(attendancePolicies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching attendance policies.");
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - GET single holiday by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _logger.LogInformation("Fetching attendance policy with ID {Id}...", id);

                var attendancePolicy = await _dbContext.AttendancePolicyMasters
                                                       .FirstOrDefaultAsync(e => e.Id == id);

                if (attendancePolicy == null)
                {
                    _logger.LogWarning("Attendance policy with ID {Id} not found.", id);
                    return NotFound($"Attendance policy with ID {id} not found.");
                }

                return Ok(attendancePolicy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching attendance policy with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - POST create a new attendance policy
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AttendancePolicyMaster newAttendancePolicy)
        {
            try
            {
                if (newAttendancePolicy == null)
                {
                    _logger.LogWarning("Create failed: Attendance policy data is null.");
                    return BadRequest("Attendance policy data is required.");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create failed: Invalid model state.");
                    return BadRequest(ModelState);
                }

                newAttendancePolicy.CreatedOn = DateTime.UtcNow;
                newAttendancePolicy.UpdatedOn = DateTime.UtcNow;

                await _dbContext.AttendancePolicyMasters.AddAsync(newAttendancePolicy);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Attendance policy created successfully with ID {Id}.", newAttendancePolicy.Id);

                return CreatedAtAction(nameof(GetById), new { id = newAttendancePolicy.Id }, newAttendancePolicy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a new attendance policy.");
                return StatusCode(500, "Internal server error");
            }
        }

        // EXISTING - PUT update a attendance policy by ID
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AttendancePolicyMaster updateAttendancePolicy)
        {
            try
            {
                if (updateAttendancePolicy == null)
                {
                    _logger.LogWarning("Update failed: Attendance policy data is null.");
                    return BadRequest("Attendance policy data is required.");
                }

                if (id != updateAttendancePolicy.Id)
                {
                    _logger.LogWarning("Update failed: ID mismatch. Route ID: {RouteId}, Body ID: {BodyId}", id, updateAttendancePolicy.Id);
                    return BadRequest("Attendance policy ID mismatch.");
                }

                var existingAttendancePolicy = await _dbContext.AttendancePolicyMasters
                    .Include(p => p.Rules)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (existingAttendancePolicy == null)
                {
                    _logger.LogWarning("Attendance policy with ID {Id} not found.", id);
                    return NotFound($"Attendance policy with ID {id} not found.");
                }

                existingAttendancePolicy.PolicyName = updateAttendancePolicy.PolicyName;
                existingAttendancePolicy.PolicyType = updateAttendancePolicy.PolicyType;
                existingAttendancePolicy.Priority = updateAttendancePolicy.Priority;
                existingAttendancePolicy.IsEnable = updateAttendancePolicy.IsEnable;
                existingAttendancePolicy.UpdatedOn = DateTime.UtcNow;   // Always set server-side

                if (existingAttendancePolicy.Rules.Count > 0)
                {
                    // Delete old rules
                    _dbContext.AttendancePolicyRules.RemoveRange(existingAttendancePolicy.Rules);
                }

                // Insert new rules
                if (updateAttendancePolicy.Rules != null && updateAttendancePolicy.Rules.Any())
                {
                    foreach (var rule in updateAttendancePolicy.Rules)
                    {
                        var newRule = new AttendancePolicyRule
                        {
                            AttendancePolicyId = existingAttendancePolicy.Id,
                            RuleCode = rule.RuleCode,
                            RuleValue = rule.RuleValue,
                        };
                        await _dbContext.AttendancePolicyRules.AddAsync(newRule);
                    }
                }
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Attendance policy with ID {Id} updated successfully.", id);
                return Ok(existingAttendancePolicy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating attendance policy with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - DELETE a attendance policy by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting attendance policy with ID {Id}...", id);

                var attendancePolicy = await _dbContext.AttendancePolicyMasters
                                                        .FirstOrDefaultAsync(e => e.Id == id);

                if (attendancePolicy == null)
                {
                    _logger.LogWarning("Delete failed: Attendance policy with ID {Id} not found.", id);
                    return NotFound($"Attendance policy with ID {id} not found.");
                }

                _dbContext.AttendancePolicyMasters.Remove(attendancePolicy);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Attendance policy with ID {Id} deleted successfully.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting attendance policy with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
