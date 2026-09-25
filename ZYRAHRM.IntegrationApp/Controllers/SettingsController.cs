using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AttendanceDbContext _context;

        public SettingsController(AttendanceDbContext context)
        {
            _context = context;
        }

        // GET: api/settings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Settings>>> GetSettings()
        {
            var settings = await _context.HRMSettings
                .AsNoTracking()
                .ToListAsync();

            return Ok(settings);
        }

        // GET: api/settings/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Settings>> GetSetting(int id)
        {
            var setting = await _context.HRMSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (setting == null)
            {
                return NotFound($"Setting with Id {id} was not found.");
            }

            return Ok(setting);
        }

        // GET: api/settings/by-name/{name}
        [HttpGet("by-name/{name}")]
        public async Task<ActionResult<Settings>> GetSettingByName(string name)
        {
            var setting = await _context.HRMSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SettingsName == name);

            if (setting == null)
            {
                return NotFound($"Setting '{name}' was not found.");
            }

            return Ok(setting);
        }

        // POST: api/settings
        [HttpPost]
        public async Task<ActionResult<Settings>> CreateSetting(Settings setting)
        {
            var exists = await _context.HRMSettings
                .AnyAsync(x => x.SettingsName == setting.SettingsName);

            if (exists)
            {
                return Conflict(
                    $"A setting with the name '{setting.SettingsName}' already exists.");
            }

            setting.Id = 0;

            _context.HRMSettings.Add(setting);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetSetting),
                new { id = setting.Id },
                setting);
        }

        // PUT: api/settings/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSetting(
            int id,
            Settings setting)
        {
            if (id != setting.Id)
            {
                return BadRequest("The Id in the URL does not match the Id in the request.");
            }

            var existingSetting = await _context.HRMSettings
                .FirstOrDefaultAsync(x => x.Id == id);

            if (existingSetting == null)
            {
                return NotFound($"Setting with Id {id} was not found.");
            }

            var duplicateName = await _context.HRMSettings
                .AnyAsync(x =>
                    x.SettingsName == setting.SettingsName &&
                    x.Id != id);

            if (duplicateName)
            {
                return Conflict(
                    $"A setting with the name '{setting.SettingsName}' already exists.");
            }

            existingSetting.SettingsName = setting.SettingsName;
            existingSetting.SettingsValue = setting.SettingsValue;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/settings/{id}
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> UpdateSettingValue(int id,[FromBody] string settingsValue)
        {
            var setting = await _context.HRMSettings
                .FirstOrDefaultAsync(x => x.Id == id);

            if (setting == null)
            {
                return NotFound($"Setting with Id {id} was not found.");
            }

            setting.SettingsValue = settingsValue;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/settings/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSetting(int id)
        {
            var setting = await _context.HRMSettings
                .FirstOrDefaultAsync(x => x.Id == id);

            if (setting == null)
            {
                return NotFound($"Setting with Id {id} was not found.");
            }

            _context.HRMSettings.Remove(setting);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}