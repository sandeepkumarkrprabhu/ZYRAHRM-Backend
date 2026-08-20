using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<AttendanceController> _logger;


        public AttendanceController(AttendanceDbContext dbContext, ILogger<AttendanceController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogInformation("Fetching Attendance logs...");

                var attendanceLogs = await _dbContext.AttendanceLogs.ToListAsync();

                if (attendanceLogs == null || !attendanceLogs.Any())
                {
                    var msgInfo = "No attendance logs found.";
                    _logger.LogWarning(msgInfo);
                    return NotFound(msgInfo);
                }

                _logger.LogInformation("Fetched {Count} attendance logs.", attendanceLogs.Count);
                return Ok(attendanceLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employee attendance.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("by-employee")]
        public async Task<IActionResult> GetByEmployee([FromQuery] string? employeeCode)
        {
            try
            {
                _logger.LogInformation("Fetching Attendance logs...");

                IQueryable<AttendanceLog> query = _dbContext.AttendanceLogs;

                // Apply filter if employeeCode is provided
                if (!string.IsNullOrWhiteSpace(employeeCode))
                {
                    query = query.Where(log => log.EmployeeCode == employeeCode);
                    _logger.LogInformation("Filtering logs for EmployeeCode: {EmployeeCode}", employeeCode);
                }

                var attendanceLogs = await query.ToListAsync();

                if (attendanceLogs == null || !attendanceLogs.Any())
                {
                    var msgInfo = string.IsNullOrWhiteSpace(employeeCode)
                        ? "No attendance logs found."
                        : $"No attendance logs found for EmployeeCode: {employeeCode}";

                    _logger.LogWarning(msgInfo);
                    return NotFound(msgInfo);
                }

                _logger.LogInformation("Fetched {Count} attendance logs.", attendanceLogs.Count);
                return Ok(attendanceLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employee attendance.");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}
