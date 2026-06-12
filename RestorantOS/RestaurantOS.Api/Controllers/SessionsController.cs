using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOS.Api.Models;
using RestaurantOS.Api.Services;
using RestaurantOS.Application.Interfaces;

namespace RestaurantOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService) => _sessionService = sessionService;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var session = await _sessionService.GetSessionDetailAsync(id, ct);
        return session == null ? NotFound(new { error = "Oturum bulunamadı." }) : Ok(session);
    }

    [HttpPost("open")]
    public async Task<IActionResult> Open([FromBody] OpenSessionRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var session = await _sessionService.OpenSessionAsync(request.TableId, request.GuestCount, userId, ct);
        return Ok(session);
    }

    [HttpPost("{id:guid}/orders")]
    public async Task<IActionResult> AddOrder(Guid id, [FromBody] AddOrderRequest request, CancellationToken ct)
    {
        var session = await _sessionService.AddOrderItemAsync(
            id, request.MenuItemId, request.Quantity, User.GetUserId(),
            request.Notes, Array.Empty<byte>(), ct);
        return Ok(session);
    }

    [HttpDelete("{sessionId:guid}/orders/{orderItemId:guid}")]
    public async Task<IActionResult> RemoveOrder(Guid sessionId, Guid orderItemId, CancellationToken ct)
    {
        var session = await _sessionService.RemoveOrderItemAsync(
            orderItemId, User.GetUserId(), "İptal", User.IsAdmin(), Array.Empty<byte>(), ct);
        return Ok(session);
    }

    [HttpPost("{id:guid}/bill")]
    public async Task<IActionResult> RequestBill(Guid id, CancellationToken ct)
    {
        var session = await _sessionService.RequestBillAsync(id, Array.Empty<byte>(), ct);
        return Ok(session);
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] PaymentRequest request, CancellationToken ct)
    {
        var session = await _sessionService.RecordPaymentAsync(
            id, request.Amount, request.Method, User.GetUserId(),
            request.ChangeGiven, request.ReferenceNo, Array.Empty<byte>(), ct);
        return Ok(session);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _sessionService.CancelSessionAsync(id, User.GetUserId(), "İptal edildi", Array.Empty<byte>(), ct);
        return Ok(new { success = true });
    }
}
