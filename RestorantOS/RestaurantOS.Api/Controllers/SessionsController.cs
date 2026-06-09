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
        var detail = await _sessionService.GetSessionDetailAsync(id, ct)
            ?? throw new Application.Exceptions.BusinessException("Oturum bulunamadı.");

        var session = await _sessionService.AddOrderItemAsync(
            id, request.MenuItemId, request.Quantity, User.GetUserId(),
            request.Notes, detail.RowVersion, ct);
        return Ok(session);
    }

    [HttpDelete("{sessionId:guid}/orders/{orderItemId:guid}")]
    public async Task<IActionResult> RemoveOrder(Guid sessionId, Guid orderItemId, CancellationToken ct)
    {
        var detail = await _sessionService.GetSessionDetailAsync(sessionId, ct)
            ?? throw new Application.Exceptions.BusinessException("Oturum bulunamadı.");

        var item = detail.OrderItems.FirstOrDefault(i => i.OrderItemId == orderItemId)
            ?? throw new Application.Exceptions.BusinessException("Sipariş bulunamadı.");

        var session = await _sessionService.RemoveOrderItemAsync(
            orderItemId, User.GetUserId(), "İptal", User.IsAdmin(), item.RowVersion, ct);
        return Ok(session);
    }

    [HttpPost("{id:guid}/bill")]
    public async Task<IActionResult> RequestBill(Guid id, CancellationToken ct)
    {
        var detail = await _sessionService.GetSessionDetailAsync(id, ct)
            ?? throw new Application.Exceptions.BusinessException("Oturum bulunamadı.");

        var session = await _sessionService.RequestBillAsync(id, detail.RowVersion, ct);
        return Ok(session);
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] PaymentRequest request, CancellationToken ct)
    {
        var detail = await _sessionService.GetSessionDetailAsync(id, ct)
            ?? throw new Application.Exceptions.BusinessException("Oturum bulunamadı.");

        var session = await _sessionService.RecordPaymentAsync(
            id, request.Amount, request.Method, User.GetUserId(),
            request.ChangeGiven, request.ReferenceNo, detail.RowVersion, ct);
        return Ok(session);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var detail = await _sessionService.GetSessionDetailAsync(id, ct)
            ?? throw new Application.Exceptions.BusinessException("Oturum bulunamadı.");

        await _sessionService.CancelSessionAsync(id, User.GetUserId(), "İptal edildi", detail.RowVersion, ct);
        return Ok(new { success = true });
    }

}
