using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Application.DTOs.Floor;

public class TableCardDto
{
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Section { get; set; } = string.Empty;
    public TableDisplayStatus Status { get; set; }
    public Guid? SessionId { get; set; }
    public int GuestCount { get; set; }
    public DateTime? OpenedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string? WaiterInitials { get; set; }
    public byte[]? RowVersion { get; set; }
}
