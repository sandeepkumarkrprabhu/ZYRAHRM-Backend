using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NavigationMenusController : ControllerBase
    {

        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<NavigationMenusController> _logger;

        public NavigationMenusController(AttendanceDbContext dbContext, ILogger<NavigationMenusController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        //  EXISTING - GET all holidays
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogInformation("Fetching navigation menus...");

                var navigationMenus = await _dbContext.NavigationMenus.ToListAsync();

                if (navigationMenus == null || !navigationMenus.Any())
                {
                    var msgInfo = "No navigation menus found.";
                    _logger.LogWarning(msgInfo);
                    return NotFound(msgInfo);
                }

                _logger.LogInformation("Fetched {Count} navigation menus.", navigationMenus.Count);
                return Ok(navigationMenus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching navigation menus.");
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - GET single navigation menu by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _logger.LogInformation("Fetching navigation menu with ID {Id}...", id);

                var navigationMenu = await _dbContext.NavigationMenus
                                                      .FirstOrDefaultAsync(e => e.MenuId== id);

                if (navigationMenu == null)
                {
                    _logger.LogWarning("Navigation menu with ID {Id} not found.", id);
                    return NotFound($"Navigation menu with ID {id} not found.");
                }

                return Ok(navigationMenu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching navigation menu with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - POST create a new navigation menu
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NavigationMenus newNavigationMenu)
        {
            try
            {
                if (newNavigationMenu == null)
                {
                    _logger.LogWarning("Create failed: Navigation menu data is null.");
                    return BadRequest("Navigation menu data is required.");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create failed: Invalid model state.");
                    return BadRequest(ModelState);
                }

                if (await _dbContext.NavigationMenus.AnyAsync(e => e.MenuId == newNavigationMenu.MenuId))
                {
                    _logger.LogWarning("Create failed: Navigation menu with ID {Id} already exists.", newNavigationMenu.MenuId);
                    return BadRequest("Navigation menu with this ID already exists.");
                }
                else
                {

                    await _dbContext.NavigationMenus.AddAsync(newNavigationMenu);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Navigation menu created successfully with ID {Id}.", newNavigationMenu.MenuId);

                    return CreatedAtAction(nameof(GetById), new { id = newNavigationMenu.MenuId }, newNavigationMenu);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a new Menu.");
                return StatusCode(500, "Internal server error");
            }
        }

        // EXISTING - PUT update a holiday
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] NavigationMenus updateNavigationMenu)
        {
            try
            {
                if (updateNavigationMenu == null)
                {
                    _logger.LogWarning("Update failed: Navigation menu data is null.");
                    return BadRequest("Navigation menu data is required.");
                }

                if (id != updateNavigationMenu.MenuId)
                {
                    _logger.LogWarning("Update failed: ID mismatch. Route ID: {RouteId}, Body ID: {BodyId}", id, updateNavigationMenu.MenuId);
                    return BadRequest("Navigation menu ID mismatch.");
                }

                var existingNavigationMenu = await _dbContext.NavigationMenus
                                                              .FirstOrDefaultAsync(e => e.MenuId == id);

                if (existingNavigationMenu == null)
                {
                    _logger.LogWarning("Navigation menu with ID {Id} not found.", id);
                    return NotFound($"Navigation menu with ID {id} not found.");
                }

                existingNavigationMenu.MenuKey = updateNavigationMenu.MenuKey;
                existingNavigationMenu.MenuLabel = updateNavigationMenu.MenuLabel;
                existingNavigationMenu.IconName = updateNavigationMenu.IconName;
                existingNavigationMenu.Description = updateNavigationMenu.Description;
                existingNavigationMenu.DisplayOrder = updateNavigationMenu.DisplayOrder;
                existingNavigationMenu.RequiredPermissionCode = updateNavigationMenu.RequiredPermissionCode;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Navigation menu with ID {Id} updated successfully.", id);
                return Ok(existingNavigationMenu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating navigation menu with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // NEW - DELETE a navigation menu by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting navigation menu with ID {Id}...", id);

                var navigationMenu = await _dbContext.NavigationMenus
                                                      .FirstOrDefaultAsync(e => e.MenuId == id);

                if (navigationMenu == null)
                {
                    _logger.LogWarning("Delete failed: Navigation menu with ID {Id} not found.", id);
                    return NotFound($"Navigation menu with ID {id} not found.");
                }

                _dbContext.NavigationMenus.Remove(navigationMenu        );
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
