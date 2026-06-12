using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) => _orderService = orderService;

    [HttpGet("kanban")]
    public async Task<IActionResult> GetKanban([FromQuery] string? section, CancellationToken ct)
        => Ok(await _orderService.GetKanbanOrdersAsync(section, null, ct));

    [HttpPost("{orderItemId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid orderItemId, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        await _orderService.UpdateOrderStatusAsync(orderItemId, request.Status, request.RowVersion, ct);
        return Ok(new { success = true });
    }
}

public record UpdateStatusRequest(OrderItemStatus Status, byte[] RowVersion);
