using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<EmployeeController> _logger;
        public EmployeeController(AttendanceDbContext dbContext, ILogger<EmployeeController> logger)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogInformation("Fetching employee mappings...");

                var employees = await _dbContext.EmployeeMappings.ToListAsync();

                if (employees == null || !employees.Any())
                {
                    _logger.LogWarning("No employee mappings found.");
                    return NotFound("No employee mappings found.");
                }

                _logger.LogInformation("Fetched {Count} employee mappings.", employees.Count);

                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employee mappings.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeMapping updatedEmployee)
        {
            try
            {
                if (updatedEmployee == null)
                {
                    _logger.LogWarning("Update failed: Employee data is null.");
                    return BadRequest("Employee data is required.");
                }

                if (id != updatedEmployee.Id)
                {
                    _logger.LogWarning("Update failed: ID mismatch. Route ID: {RouteId}, Body ID: {BodyId}", id, updatedEmployee.Id);
                    return BadRequest("Employee ID mismatch.");
                }

                var existingEmployee = await _dbContext.EmployeeMappings
                                                       .FirstOrDefaultAsync(e => e.Id == id);

                if (existingEmployee == null)
                {
                    _logger.LogWarning("Employee with ID {Id} not found.", id);
                    return NotFound($"Employee with ID {id} not found.");
                }

                // Update fields (map only what is needed)
                existingEmployee.HRMEmployeeCode = updatedEmployee.HRMEmployeeCode;
                existingEmployee.EmployeeName = updatedEmployee.EmployeeName;
                //existingEmployee.BiometricUserId = updatedEmployee.BiometricUserId;
                existingEmployee.IsActive = updatedEmployee.IsActive;
                existingEmployee.IsExcludeFromBiometric = updatedEmployee.IsExcludeFromBiometric;
                existingEmployee.IsCheckoutFinalOverriddenByHR = updatedEmployee.IsCheckoutFinalOverriddenByHR;
                existingEmployee.LastCheckoutFinal = updatedEmployee.LastCheckoutFinal;
                existingEmployee.UpdatedDateTime = DateTime.Now;    

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Employee with ID {Id} updated successfully.", id);

                return Ok(existingEmployee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating employee with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
