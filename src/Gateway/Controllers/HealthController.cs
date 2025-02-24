using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        // GET: api/health
        [HttpGet]
        public IActionResult Get()
        {
            // Повертаємо просту відповідь для перевірки стану Gateway
            return Ok(new { Status = "Gateway працює", Timestamp = DateTime.UtcNow });
        }
    }
}
