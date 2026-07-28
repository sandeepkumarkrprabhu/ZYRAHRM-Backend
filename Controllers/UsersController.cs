using Microsoft.AspNetCore.Mvc;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AttendanceDbContext dbContext, ILogger<UsersController> logger)
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
                CreatePasswordHash(plainPassword, out byte[] passwordHash, out byte[] passwordSalt);
                newUser.PasswordHash = Convert.ToBase64String(passwordHash);
                newUser.PasswordSalt = Convert.ToBase64String(passwordSalt);

                newUser.CreatedDateTime = DateTime.UtcNow;
                newUser.LastLoginDateTime = DateTime.UtcNow;
                newUser.Status = true;
                newUser.FailedLoginAttempts = 0;
                newUser.IsLocked = false;
                newUser.IsPasswordResetRequired = true; // force reset on first login

                await _dbContext.Users.AddAsync(newUser);
                await _dbContext.SaveChangesAsync();

                // Send email with plain password
                //await SendPasswordEmail(newUser.Email, plainPassword);

                _logger.LogInformation("User created successfully with ID {Id}.", newUser.Id);
                return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, newUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a new user.");
                return StatusCode(500, "Internal server error");
            }
        }


        // Helper for password hashing
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        //Helper for random password generation
        private string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?";
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var chars = randomBytes.Select(b => validChars[b % validChars.Length]);
            return new string(chars.ToArray());
        }
    }
}
