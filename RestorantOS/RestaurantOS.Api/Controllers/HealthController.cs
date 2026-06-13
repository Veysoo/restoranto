using Microsoft.AspNetCore.Mvc;

namespace RestaurantOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", time = DateTime.UtcNow });

    [HttpGet("network")]
    public IActionResult GetNetwork()
    {
        var hostIp = Environment.GetEnvironmentVariable("HOST_LAN_IP") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("EXTERNAL_PORT") ?? "8080";
        var url = $"http://{hostIp}:{port}";
        return Ok(new { ip = hostIp, url });
    }
}
