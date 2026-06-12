namespace RestaurantOS.Domain.Entities;

public class MenuItem : BaseEntity
{
    public Guid MenuItemId { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal TaxRate { get; set; } = 10.00m;
    public bool IsAvailable { get; set; } = true;
    public int PrepTimeMinutes { get; set; } = 10;
    public string? ImagePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public MenuCategory Category { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
