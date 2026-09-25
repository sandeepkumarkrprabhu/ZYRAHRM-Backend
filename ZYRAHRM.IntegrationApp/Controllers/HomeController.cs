using Microsoft.AspNetCore.Mvc;

namespace ZYRAHRM.IntegrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpGet("all")]
        public IActionResult GetAll()
        {
            return Ok();
        }
    }
}
