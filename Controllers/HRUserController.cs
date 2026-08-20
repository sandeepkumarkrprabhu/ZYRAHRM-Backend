using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZYRA.Attendance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HRUserController : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<HRUserController> _logger;

        public HRUserController(AttendanceDbContext dbContext, ILogger<HRUserController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET all users
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var users = await _dbContext.Users.ToListAsync();

                if (users == null || !users.Any())
                    return NotFound("No users found.");

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
