using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
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

        private readonly IEmailService _emailService;

        public AuthController(AttendanceDbContext dbContext, ILogger<AuthController> logger, IEmailService emailService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _emailService = emailService;
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


        [Authorize]
        [HttpPost("changepassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Password data is required.");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Get user from database
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(x => x.Id == Convert.ToInt32(request.UserId));

                if (user == null)
                    return NotFound("User not found.");

                // Verify current password
                var currentPasswordValid =
                    PasswordHelper.VerifyPasswordHash(
                        request.CurrentPassword,
                        user.PasswordHash,
                        user.PasswordSalt);

                if (!currentPasswordValid)
                    return BadRequest("Current password is incorrect.");

                // Optional: prevent using the same password
                var samePassword =
                    PasswordHelper.VerifyPasswordHash(
                        request.NewPassword,
                        user.PasswordHash,
                        user.PasswordSalt);

                if (samePassword)
                    return BadRequest("New password must be different from the current password.");

                // Create new password hash + salt
                PasswordHelper.CreatePasswordHash(
                    request.NewPassword,
                    out byte[] passwordHash,
                    out byte[] passwordSalt);

                // Update password
                user.PasswordHash = Convert.ToBase64String(passwordHash);
                user.PasswordSalt = Convert.ToBase64String(passwordSalt);

                // Password has now been changed
                user.IsPasswordResetRequired = false;

                // Optional security fields
                user.FailedLoginAttempts = 0;
                user.IsLocked = false;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Password changed successfully for user ID {UserId}.",
                    user.Id);

                // Send password email
                try
                {
                    var emailMessage = new EmailMessage
                    {
                        To = user.Email,
                        ToName = user.FullName,
                        Subject = "Your ZYRA HRM password has been changed",

                        PlainTextBody =
                            $"Hi {user.FullName},\n\n" +
                            $"Your ZYRA HRM password has been changed successfully.\n\n" +
                            $"Your new password is: {request.NewPassword}\n\n" +
                            $"Login: https://192.168.1.15:6369\n\n" +
                            $"If you did not make this change, please contact your administrator immediately.",

                        HtmlBody =
                            $"<p>Hi <strong>{user.FullName}</strong>,</p>" +
                            $"<p>Your ZYRA HRM password has been changed successfully.</p>" +
                            $"<p><strong>New Password:</strong> {request.NewPassword}</p>" +
                            $"<p><a href='https://192.168.1.15:6369'>Login to ZYRA HRM</a></p>" +
                            $"<p>If you did not make this change, please contact your administrator immediately.</p>"
                    };

                    //await _emailService.SendAsync(emailMessage);
                }
                catch (Exception ex)
                {
                    // Password was changed successfully.
                    // Email failure should not undo the password change.
                    _logger.LogError(
                        ex,
                        "Password changed but failed to send password email to {Email}.",
                        user.Email);
                }

                return Ok(new
                {
                    message = "Password changed successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while changing password.");

                return StatusCode(500, "Internal server error.");
            }
        }


    }


}
