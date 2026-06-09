using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOS.Application.Interfaces;

namespace RestaurantOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FloorController : ControllerBase
{
    private readonly IFloorService _floorService;

    public FloorController(IFloorService floorService) => _floorService = floorService;

    [HttpGet("sections")]
    public async Task<IActionResult> GetSections(CancellationToken ct)
        => Ok(await _floorService.GetSectionsAsync(ct));

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables([FromQuery] string? section, CancellationToken ct)
        => Ok(await _floorService.GetTablesAsync(section, ct));
}
