using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Application.DTOs.Settings;

public class AppSettingsDto
{
    public string RestaurantName { get; set; } = "RestaurantOS";
    public decimal DefaultTaxRate { get; set; } = 10m;
    public string AccentColor { get; set; } = "#4F6EF7";
}

public class TableSettingsDto
{
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Section { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class UserSettingsDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}
