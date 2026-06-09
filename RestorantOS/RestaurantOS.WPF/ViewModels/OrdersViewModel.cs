using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOS.Application.DTOs.Orders;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Enums;
using RestaurantOS.WPF.Services;

namespace RestaurantOS.WPF.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    private readonly IOrderService _orderService;
    private readonly ToastService _toast;

    [ObservableProperty] private ObservableCollection<OrderKanbanDto> _pending = new();
    [ObservableProperty] private ObservableCollection<OrderKanbanDto> _preparing = new();
    [ObservableProperty] private ObservableCollection<OrderKanbanDto> _served = new();
    [ObservableProperty] private ObservableCollection<OrderKanbanDto> _cancelled = new();
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _selectedSection = "Tümü";

    public OrdersViewModel(IOrderService orderService, ToastService toast)
    {
        _orderService = orderService;
        _toast = toast;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var orders = await _orderService.GetKanbanOrdersAsync(SelectedSection == "Tümü" ? null : SelectedSection);
            Pending = new ObservableCollection<OrderKanbanDto>(orders.Where(o => o.Status == OrderItemStatus.Pending));
            Preparing = new ObservableCollection<OrderKanbanDto>(orders.Where(o => o.Status == OrderItemStatus.Preparing));
            Served = new ObservableCollection<OrderKanbanDto>(orders.Where(o => o.Status == OrderItemStatus.Served));
            Cancelled = new ObservableCollection<OrderKanbanDto>(orders.Where(o => o.Status == OrderItemStatus.Cancelled));
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task MoveToPreparingAsync(OrderKanbanDto? order)
    {
        if (order == null) return;
        await UpdateStatusAsync(order, OrderItemStatus.Preparing);
    }

    [RelayCommand]
    private async Task MoveToServedAsync(OrderKanbanDto? order)
    {
        if (order == null) return;
        await UpdateStatusAsync(order, OrderItemStatus.Served);
    }

    [RelayCommand]
    private async Task MoveToCancelledAsync(OrderKanbanDto? order)
    {
        if (order == null) return;
        await UpdateStatusAsync(order, OrderItemStatus.Cancelled);
    }

    private async Task UpdateStatusAsync(OrderKanbanDto order, OrderItemStatus status)
    {
        try
        {
            await _orderService.UpdateOrderStatusAsync(order.OrderItemId, status, order.RowVersion);
            await LoadAsync();
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
    }
}
