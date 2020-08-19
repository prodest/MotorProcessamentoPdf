using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class HomeController : ControllerBase
    {
        [HttpGet("/health/live")]
        public IActionResult Health()
        {
            return Ok();
        }
    }
}
