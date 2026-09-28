using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/navigation")]
    [ApiController]
    [Authorize]
    public class NavigationController : ControllerBase
    {
        private readonly INavigationService _navigationService;
        private readonly ILogger<NavigationController> _logger;

        public NavigationController(
            INavigationService navigationService,
            ILogger<NavigationController> logger)
        {
            _navigationService = navigationService;
            _logger = logger;
        }

        // GET: api/navigation/menus/{userId}
        [HttpGet("menus/{userId:long}")]
        public async Task<ActionResult<List<NavigationMenuDto>>> GetMenusByUserId(long userId)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching granted navigation menus for user {UserId}.",
                    userId);

                var authenticatedUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!long.TryParse(authenticatedUserId, out var currentUserId))
            {
                return Unauthorized(new { message = "Invalid authenticated user." });
            }

            if (currentUserId != userId)
            {
                return Forbid();
            }

            var menus = await _navigationService.GetMenusByUserIdAsync(userId);

                if (menus.Count == 0)
                {
                    return NotFound(
                        new
                        {
                            message = "No navigation menus found for the specified user."
                        });
                }

                return Ok(menus);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while fetching navigation menus for user {UserId}.",
                    userId);

                return StatusCode(500, "Internal server error");
            }
        }
    }
}
