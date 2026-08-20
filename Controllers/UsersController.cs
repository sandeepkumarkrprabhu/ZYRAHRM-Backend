using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NETCore.MailKit.Core;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;
using ZYRAHRM.IntegrationApp.Helper;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<UsersController> _logger;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public UsersController(AttendanceDbContext dbContext, ILogger<UsersController> logger, EmailService emailService, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
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

        // GET single user by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Id == id);

                if (user == null)
                    return NotFound($"User with ID {id} not found.");

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching user with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST create a new user with password hashing
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Users newUser)
        {
            try
            {
                if (newUser == null)
                    return BadRequest("User data is required.");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Generate random password
                var plainPassword = newUser.PasswordHash;

                // Hash + Salt
                PasswordHelper.CreatePasswordHash(plainPassword, out byte[] passwordHash, out byte[] passwordSalt);
                newUser.PasswordHash = Convert.ToBase64String(passwordHash);
                newUser.PasswordSalt = Convert.ToBase64String(passwordSalt);

                newUser.CreatedDateTime = DateTime.UtcNow;
                newUser.LastLoginDateTime = DateTime.UtcNow;
                newUser.Status = true;
                newUser.FailedLoginAttempts = 0;
                newUser.IsLocked = false;
                newUser.RoleName = newUser.RoleName;
                newUser.IsPasswordResetRequired = true; // force reset on first login

                await _dbContext.Users.AddAsync(newUser);
                await _dbContext.SaveChangesAsync();

                // Send plain-text email with credentials
                //try
                //{
                //    await _emailService.SendAsync(newUser.Email, newUser.FullName, plainPassword);
                //}
                //catch (Exception ex)
                //{
                //    _logger.LogError(ex, "Failed to send password email to {Email}.", newUser.Email);
                //    // Decide: you can still return Created or return 500 depending on policy
                //}

                _logger.LogInformation("User created successfully with ID {Id}.", newUser.Id);
                return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, newUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a new user.");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}
