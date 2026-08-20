using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.PresentationModels;
using ZYRAHRM.IntegrationApp.Helper;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AttendanceDbContext dbContext, ILogger<AuthController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Username);

            if (user == null || !PasswordHelper.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Invalid login attempt for {Username}", request.Username);
                return Unauthorized(new { message = "Invalid username or password" });
            }

            // Generate JWT or session token (simplified here)
            var token = JwtTokenHelper.GenerateToken(user);

            return Ok(new
            {
                token,
                user = new { user.Id, user.FullName, user.Email }
            });
        }

        // GET: api/auth/me
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { message = "Not authenticated" });

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsLocked = user.IsLocked,
                Status = user.Status,
                RoleName = user.RoleName
            });
        }

    }


}
