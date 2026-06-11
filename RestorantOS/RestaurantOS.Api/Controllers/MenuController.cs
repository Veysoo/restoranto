using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOS.Application.DTOs.Menu;
using RestaurantOS.Application.Interfaces;

namespace RestaurantOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService) => _menuService = menuService;

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
        => Ok(await _menuService.GetCategoriesWithItemsAsync(ct));

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken ct)
        => Ok(await _menuService.CreateCategoryAsync(request.Name, request.Icon ?? "🍽️", ct));

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] MenuItemDto item, CancellationToken ct)
        => Ok(await _menuService.CreateMenuItemAsync(item, ct));

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] MenuItemDto item, CancellationToken ct)
    {
        item.MenuItemId = id;
        return Ok(await _menuService.UpdateMenuItemAsync(item, ct));
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct)
    {
        await _menuService.DeleteMenuItemAsync(id, ct);
        return Ok(new { success = true });
    }

    [HttpPost("items/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleAvailability(Guid id, CancellationToken ct)
    {
        await _menuService.ToggleAvailabilityAsync(id, ct);
        return Ok(new { success = true });
    }

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        await _menuService.DeleteCategoryAsync(id, ct);
        return Ok(new { success = true });
    }
}

public record CreateCategoryRequest(string Name, string? Icon);
