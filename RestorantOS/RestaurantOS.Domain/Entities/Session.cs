using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Domain.Entities;

public class Session : BaseEntity
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Guid TableId { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public int GuestCount { get; set; } = 1;
    public SessionStatus Status { get; set; } = SessionStatus.Open;
    public Guid OpenedByUserId { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalAmount { get; set; }

    public Table Table { get; set; } = null!;
    public User OpenedByUser { get; set; } = null!;
    public User? ClosedByUser { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
