using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderItemId { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public OrderItemStatus Status { get; set; } = OrderItemStatus.Pending;
    public DateTime? SentToKitchenAt { get; set; }
    public DateTime? ServedAt { get; set; }
    public Guid AddedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Session Session { get; set; } = null!;
    public MenuItem MenuItem { get; set; } = null!;
    public User AddedByUser { get; set; } = null!;
}
