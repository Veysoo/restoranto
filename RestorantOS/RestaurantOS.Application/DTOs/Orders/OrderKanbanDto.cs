using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Application.DTOs.Orders;

public class OrderKanbanDto
{
    public Guid OrderItemId { get; set; }
    public Guid SessionId { get; set; }
    public int TableNumber { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public OrderItemStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string WaiterName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
