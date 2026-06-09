namespace RestaurantOS.Application.DTOs.Menu;

public class MenuCategoryDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<MenuItemDto> Items { get; set; } = new();
}

public class MenuItemDto
{
    public Guid MenuItemId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsAvailable { get; set; }
    public int PrepTimeMinutes { get; set; }
    public string? ImagePath { get; set; }
}
