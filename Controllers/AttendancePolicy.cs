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

                var attendancePolicies = await _dbContext.AttendancePolicyMasters.ToListAsync();

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
                _logger.LogInformation("Fetching holiday with ID {Id}...", id);

                var holiday = await _dbContext.Holidiays
                                              .FirstOrDefaultAsync(e => e.Id == id);

                if (holiday == null)
                {
                    _logger.LogWarning("Holiday with ID {Id} not found.", id);
                    return NotFound($"Holiday with ID {id} not found.");
                }

                return Ok(holiday);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching holiday with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - POST create a new holiday
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HolidayMaster newHoliday)
        {
            try
            {
                if (newHoliday == null)
                {
                    _logger.LogWarning("Create failed: Holiday data is null.");
                    return BadRequest("Holiday data is required.");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create failed: Invalid model state.");
                    return BadRequest(ModelState);
                }

                newHoliday.CreatedOn = DateTime.UtcNow;
                newHoliday.UpdatedOn = DateTime.UtcNow;

                await _dbContext.Holidiays.AddAsync(newHoliday);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Holiday created successfully with ID {Id}.", newHoliday.Id);

                return CreatedAtAction(nameof(GetById), new { id = newHoliday.Id }, newHoliday);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a new holiday.");
                return StatusCode(500, "Internal server error");
            }
        }

        // EXISTING - PUT update a holiday
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] HolidayMaster updateHoliday)
        {
            try
            {
                if (updateHoliday == null)
                {
                    _logger.LogWarning("Update failed: Holiday data is null.");
                    return BadRequest("Holiday data is required.");
                }

                if (id != updateHoliday.Id)
                {
                    _logger.LogWarning("Update failed: ID mismatch. Route ID: {RouteId}, Body ID: {BodyId}", id, updateHoliday.Id);
                    return BadRequest("Holiday ID mismatch.");
                }

                var existingHoldiay = await _dbContext.Holidiays
                                                       .FirstOrDefaultAsync(e => e.Id == id);

                if (existingHoldiay == null)
                {
                    _logger.LogWarning("Holiday with ID {Id} not found.", id);
                    return NotFound($"Holiday with ID {id} not found.");
                }

                existingHoldiay.HolidayCode = updateHoliday.HolidayCode;
                existingHoldiay.HolidayName = updateHoliday.HolidayName;
                existingHoldiay.HolidayType = updateHoliday.HolidayType;
                existingHoldiay.Description = updateHoliday.Description;
                existingHoldiay.IsActive = updateHoliday.IsActive;
                existingHoldiay.UpdatedOn = DateTime.UtcNow;   // Always set server-side

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Holiday with ID {Id} updated successfully.", id);
                return Ok(existingHoldiay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating holiday with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - DELETE a holiday by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting holiday with ID {Id}...", id);

                var holiday = await _dbContext.Holidiays
                                              .FirstOrDefaultAsync(e => e.Id == id);

                if (holiday == null)
                {
                    _logger.LogWarning("Delete failed: Holiday with ID {Id} not found.", id);
                    return NotFound($"Holiday with ID {id} not found.");
                }

                _dbContext.Holidiays.Remove(holiday);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Holiday with ID {Id} deleted successfully.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting holiday with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
