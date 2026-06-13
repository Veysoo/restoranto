using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOS.Application.Interfaces;

namespace RestaurantOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService) => _dashboardService = dashboardService;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _dashboardService.GetDashboardAsync(ct));

    [HttpGet("report")]
    public async Task<IActionResult> Report([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to) return BadRequest(new { error = "Başlangıç tarihi bitiş tarihinden büyük olamaz." });
        if ((to - from).TotalDays > 365) return BadRequest(new { error = "En fazla 365 günlük rapor alınabilir." });
        return Ok(await _dashboardService.GetSalesReportAsync(from, to, ct));
    }
}
