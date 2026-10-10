using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/modules")]
    [ApiController]
    public class ModulesController : ControllerBase
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<ModulesController> _logger;

        public ModulesController(AttendanceDbContext dbContext, ILogger<ModulesController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool includeInactive = false)
        {
            var query = _dbContext.Modules.AsNoTracking();
            if (!includeInactive)
                query = query.Where(x => x.IsActive);

            var modules = await query.OrderBy(x => x.ModuleName).ToListAsync();
            return Ok(modules);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var module = await _dbContext.Modules.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ModuleId == id);

            return module == null ? NotFound() : Ok(module);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Modules module)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            module.ModuleId = 0;
            module.ModuleCode = module.ModuleCode?.Trim().ToUpperInvariant();
            module.ModuleName = module.ModuleName?.Trim();

            if (string.IsNullOrWhiteSpace(module.ModuleCode) ||
                string.IsNullOrWhiteSpace(module.ModuleName))
                return BadRequest("ModuleCode and ModuleName are required.");

            if (await _dbContext.Modules.AnyAsync(x => x.ModuleCode == module.ModuleCode))
                return Conflict($"Module code '{module.ModuleCode}' already exists.");

            _dbContext.Modules.Add(module);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created module {ModuleCode} with ID {ModuleId}.", module.ModuleCode, module.ModuleId);
            return CreatedAtAction(nameof(GetById), new { id = module.ModuleId }, module);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Modules request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var module = await _dbContext.Modules.FirstOrDefaultAsync(x => x.ModuleId == id);
            if (module == null)
                return NotFound();

            var code = request.ModuleCode?.Trim().ToUpperInvariant();
            var name = request.ModuleName?.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return BadRequest("ModuleCode and ModuleName are required.");

            if (await _dbContext.Modules.AnyAsync(x => x.ModuleId != id && x.ModuleCode == code))
                return Conflict($"Module code '{code}' already exists.");

            module.ModuleCode = code;
            module.ModuleName = name;
            module.Description = request.Description?.Trim();
            module.IsActive = request.IsActive;

            await _dbContext.SaveChangesAsync();
            return Ok(module);
        }
    }
}
