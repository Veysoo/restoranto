using Microsoft.EntityFrameworkCore;
using RestaurantOS.Application.DTOs.Menu;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Entities;
using RestaurantOS.Infrastructure.Data;

namespace RestaurantOS.Infrastructure.Services;

public class MenuService : IMenuService
{
    private readonly AppDbContext _context;

    public MenuService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MenuCategoryDto>> GetCategoriesWithItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MenuCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new MenuCategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Icon = c.Icon,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                Items = c.MenuItems.Select(m => new MenuItemDto
                {
                    MenuItemId = m.MenuItemId,
                    CategoryId = m.CategoryId,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    TaxRate = m.TaxRate,
                    IsAvailable = m.IsAvailable,
                    PrepTimeMinutes = m.PrepTimeMinutes,
                    ImagePath = m.ImagePath
                }).ToList()
            }).ToListAsync(cancellationToken);
    }

    public async Task<MenuItemDto> CreateMenuItemAsync(MenuItemDto item, CancellationToken cancellationToken = default)
    {
        var entity = new MenuItem
        {
            CategoryId = item.CategoryId,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            TaxRate = item.TaxRate,
            IsAvailable = item.IsAvailable,
            PrepTimeMinutes = item.PrepTimeMinutes,
            ImagePath = item.ImagePath
        };
        _context.MenuItems.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        item.MenuItemId = entity.MenuItemId;
        return item;
    }

    public async Task<MenuItemDto> UpdateMenuItemAsync(MenuItemDto item, CancellationToken cancellationToken = default)
    {
        var entity = await _context.MenuItems.FindAsync(new object[] { item.MenuItemId }, cancellationToken);
        if (entity == null) return item;

        entity.Name = item.Name;
        entity.Description = item.Description;
        entity.Price = item.Price;
        entity.TaxRate = item.TaxRate;
        entity.IsAvailable = item.IsAvailable;
        entity.PrepTimeMinutes = item.PrepTimeMinutes;
        entity.ImagePath = item.ImagePath;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task ToggleAvailabilityAsync(Guid menuItemId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.MenuItems.FindAsync(new object[] { menuItemId }, cancellationToken);
        if (entity != null)
        {
            entity.IsAvailable = !entity.IsAvailable;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MenuCategoryDto> CreateCategoryAsync(string name, string icon, CancellationToken cancellationToken = default)
    {
        var maxOrder = await _context.MenuCategories.MaxAsync(c => (int?)c.DisplayOrder, cancellationToken) ?? 0;
        var category = new MenuCategory { Name = name, Icon = icon, DisplayOrder = maxOrder + 1 };
        _context.MenuCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return new MenuCategoryDto { CategoryId = category.CategoryId, Name = name, Icon = icon, DisplayOrder = category.DisplayOrder, IsActive = true };
    }
}
