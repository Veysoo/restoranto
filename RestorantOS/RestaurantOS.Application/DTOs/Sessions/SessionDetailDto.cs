using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Application.DTOs.Sessions;

public class SessionDetailDto
{
    public Guid SessionId { get; set; }
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public string TableName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public int GuestCount { get; set; }
    public SessionStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<OrderItemDto> OrderItems { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
}

public class OrderItemDto
{
    public Guid OrderItemId { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public OrderItemStatus Status { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class PaymentDto
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAt { get; set; }
    public decimal ChangeGiven { get; set; }
    public bool IsVoid { get; set; }
}
