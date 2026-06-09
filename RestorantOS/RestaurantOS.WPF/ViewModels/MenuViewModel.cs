using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOS.Application.DTOs.Menu;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.WPF.Services;

namespace RestaurantOS.WPF.ViewModels;

public partial class MenuViewModel : ObservableObject
{
    private readonly IMenuService _menuService;
    private readonly ToastService _toast;

    [ObservableProperty] private ObservableCollection<MenuCategoryDto> _categories = new();
    [ObservableProperty] private MenuCategoryDto? _selectedCategory;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isEditPanelOpen;
    [ObservableProperty] private MenuItemDto _editingItem = new();

    public MenuViewModel(IMenuService menuService, ToastService toast)
    {
        _menuService = menuService;
        _toast = toast;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var cats = await _menuService.GetCategoriesWithItemsAsync();
            Categories = new ObservableCollection<MenuCategoryDto>(cats);
            SelectedCategory ??= Categories.FirstOrDefault();
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void OpenAddItem()
    {
        if (SelectedCategory == null) return;
        EditingItem = new MenuItemDto { CategoryId = SelectedCategory.CategoryId, TaxRate = 10, IsAvailable = true, PrepTimeMinutes = 10 };
        IsEditPanelOpen = true;
    }

    [RelayCommand]
    private void OpenEditItem(MenuItemDto? item)
    {
        if (item == null) return;
        EditingItem = new MenuItemDto
        {
            MenuItemId = item.MenuItemId,
            CategoryId = item.CategoryId,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            TaxRate = item.TaxRate,
            IsAvailable = item.IsAvailable,
            PrepTimeMinutes = item.PrepTimeMinutes,
            ImagePath = item.ImagePath
        };
        IsEditPanelOpen = true;
    }

    [RelayCommand]
    private async Task SaveItemAsync()
    {
        try
        {
            if (EditingItem.MenuItemId == Guid.Empty)
                await _menuService.CreateMenuItemAsync(EditingItem);
            else
                await _menuService.UpdateMenuItemAsync(EditingItem);
            IsEditPanelOpen = false;
            _toast.Success("Menü kaydedildi.");
            await LoadAsync();
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
    }

    [RelayCommand]
    private async Task ToggleAvailabilityAsync(MenuItemDto? item)
    {
        if (item == null) return;
        await _menuService.ToggleAvailabilityAsync(item.MenuItemId);
        await LoadAsync();
    }

    [RelayCommand]
    private void CloseEditPanel() => IsEditPanelOpen = false;
}
