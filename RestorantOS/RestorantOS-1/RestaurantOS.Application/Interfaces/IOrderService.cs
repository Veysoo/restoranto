using RestaurantOS.Application.DTOs.Orders;
using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Application.Interfaces;

public interface IOrderService
{
    Task<IReadOnlyList<OrderKanbanDto>> GetKanbanOrdersAsync(string? section = null, Guid? waiterId = null, CancellationToken cancellationToken = default);
    Task UpdateOrderStatusAsync(Guid orderItemId, OrderItemStatus newStatus, byte[] rowVersion, CancellationToken cancellationToken = default);
}
