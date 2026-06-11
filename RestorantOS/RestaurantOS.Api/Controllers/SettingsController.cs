using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOS.Application.DTOs.Settings;
using RestaurantOS.Application.Interfaces;

namespace RestaurantOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService) => _settingsService = settingsService;

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables(CancellationToken ct)
        => Ok(await _settingsService.GetTablesAsync(ct));

    [HttpPost("tables")]
    public async Task<IActionResult> SaveTable([FromBody] TableSettingsDto table, CancellationToken ct)
        => Ok(await _settingsService.SaveTableAsync(table, ct));

    [HttpDelete("tables/{id:guid}")]
    public async Task<IActionResult> DeleteTable(Guid id, CancellationToken ct)
    {
        await _settingsService.DeleteTableAsync(id, ct);
        return Ok(new { success = true });
    }

    [HttpGet("app")]
    public async Task<IActionResult> GetAppSettings(CancellationToken ct)
        => Ok(await _settingsService.GetAppSettingsAsync(ct));

    [HttpPut("app")]
    public async Task<IActionResult> SaveAppSettings([FromBody] AppSettingsDto settings, CancellationToken ct)
    {
        await _settingsService.SaveAppSettingsAsync(settings, ct);
        return Ok(new { success = true });
    }
}
