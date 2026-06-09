using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid PaymentId { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public Guid ReceivedByUserId { get; set; }
    public decimal ChangeGiven { get; set; }
    public string? ReferenceNo { get; set; }
    public bool IsVoid { get; set; }
    public string? VoidReason { get; set; }

    public Session Session { get; set; } = null!;
    public User ReceivedByUser { get; set; } = null!;
}
