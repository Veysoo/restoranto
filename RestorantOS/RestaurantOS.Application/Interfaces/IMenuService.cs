using RestaurantOS.Application.DTOs.Menu;

namespace RestaurantOS.Application.Interfaces;

public interface IMenuService
{
    Task<IReadOnlyList<MenuCategoryDto>> GetCategoriesWithItemsAsync(CancellationToken cancellationToken = default);
    Task<MenuItemDto> CreateMenuItemAsync(MenuItemDto item, CancellationToken cancellationToken = default);
    Task<MenuItemDto> UpdateMenuItemAsync(MenuItemDto item, CancellationToken cancellationToken = default);
    Task ToggleAvailabilityAsync(Guid menuItemId, CancellationToken cancellationToken = default);
    Task<MenuCategoryDto> CreateCategoryAsync(string name, string icon, CancellationToken cancellationToken = default);
}
