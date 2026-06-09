using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOS.Application.DTOs.Menu;
using RestaurantOS.Application.DTOs.Sessions;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Enums;
using RestaurantOS.WPF.Services;

namespace RestaurantOS.WPF.ViewModels;

public partial class SessionDetailViewModel : ObservableObject
{
    private readonly Guid _sessionId;
    private readonly ISessionService _sessionService;
    private readonly IMenuService _menuService;
    private readonly CurrentUserService _currentUser;
    private readonly ToastService _toast;

    [ObservableProperty] private SessionDetailDto? _session;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _showPaymentSuccess;
    [ObservableProperty] private string _paymentSuccessMessage = string.Empty;
    [ObservableProperty] private decimal _completedPaymentAmount;
    [ObservableProperty] private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;
    [ObservableProperty] private decimal _cashTendered;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private bool _isOrderTab = true;
    [ObservableProperty] private ObservableCollection<MenuCategoryDto> _menuCategories = new();
    [ObservableProperty] private MenuCategoryDto? _selectedCategory;
    [ObservableProperty] private ObservableCollection<MenuItemDto> _menuItems = new();
    [ObservableProperty] private int _addQuantity = 1;
    [ObservableProperty] private bool _isAddingOrder;

    public Action? OnSessionClosed { get; set; }
    public Action? OnSessionUpdated { get; set; }

    public decimal ChangeAmount => Math.Max(0, CashTendered - PaymentAmount);
    public bool CanOrder => Session?.Status == SessionStatus.Open;
    public bool CanPay => Session?.Status is SessionStatus.Open or SessionStatus.Billed;

    public SessionDetailViewModel(Guid sessionId, ISessionService sessionService, IMenuService menuService,
        CurrentUserService currentUser, ToastService toast)
    {
        _sessionId = sessionId;
        _sessionService = sessionService;
        _menuService = menuService;
        _currentUser = currentUser;
        _toast = toast;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await LoadMenuAsync();
            Session = await _sessionService.GetSessionDetailAsync(_sessionId);
            if (Session == null) return;

            PaymentAmount = Session.RemainingAmount > 0 ? Session.RemainingAmount : Session.FinalAmount;

            if (Session.Status == SessionStatus.Billed)
            {
                IsOrderTab = false;
            }
            else
            {
                IsOrderTab = true;
            }
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
        finally { IsLoading = false; }
    }

    private async Task LoadMenuAsync()
    {
        var cats = await _menuService.GetCategoriesWithItemsAsync();
        MenuCategories = new ObservableCollection<MenuCategoryDto>(cats);
        SelectedCategory = cats.FirstOrDefault();
        UpdateMenuItems();
    }

    partial void OnSelectedCategoryChanged(MenuCategoryDto? value) => UpdateMenuItems();

    [RelayCommand]
    private void SelectCategory(MenuCategoryDto? category)
    {
        if (category != null) SelectedCategory = category;
    }

    private void UpdateMenuItems()
    {
        MenuItems = SelectedCategory == null
            ? new ObservableCollection<MenuItemDto>()
            : new ObservableCollection<MenuItemDto>(SelectedCategory.Items.Where(i => i.IsAvailable));
    }

    [RelayCommand]
    private void ShowOrderTab() => IsOrderTab = true;

    [RelayCommand]
    private async Task ShowPaymentTabAsync()
    {
        if (Session == null) return;

        if (Session.Status == SessionStatus.Open)
        {
            if (!Session.OrderItems.Any(i => i.Status != OrderItemStatus.Cancelled))
            {
                _toast.Warning("Önce sipariş ekleyin.");
                return;
            }
            try
            {
                Session = await _sessionService.RequestBillAsync(_sessionId, Session.RowVersion);
                PaymentAmount = Session.RemainingAmount > 0 ? Session.RemainingAmount : Session.FinalAmount;
                OnSessionUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                _toast.Error(ex.Message);
                return;
            }
        }

        IsOrderTab = false;
    }

    [RelayCommand]
    private async Task QuickAddOrderAsync(MenuItemDto? item)
    {
        if (item == null || Session == null || !CanOrder) return;
        if (IsAddingOrder) return;

        IsAddingOrder = true;
        try
        {
            Session = await _sessionService.AddOrderItemAsync(_sessionId, item.MenuItemId,
                1, _currentUser.UserId, null, Session.RowVersion);
            _toast.Success($"{item.Name} eklendi.");
            OnSessionUpdated?.Invoke();
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
        finally { IsAddingOrder = false; }
    }

    [RelayCommand]
    private async Task RemoveItemAsync(OrderItemDto? item)
    {
        if (item == null || Session == null) return;
        try
        {
            Session = await _sessionService.RemoveOrderItemAsync(item.OrderItemId, _currentUser.UserId,
                "Müşteri iptali", _currentUser.IsAdmin, item.RowVersion);
            PaymentAmount = Session.RemainingAmount > 0 ? Session.RemainingAmount : Session.FinalAmount;
            OnSessionUpdated?.Invoke();
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
    }

    [RelayCommand]
    private void SelectPaymentMethod(string method)
    {
        if (Enum.TryParse<PaymentMethod>(method, out var pm))
            SelectedPaymentMethod = pm;
    }

    partial void OnCashTenderedChanged(decimal value)
    {
        if (SelectedPaymentMethod == PaymentMethod.Cash && value > 0 && Session != null)
            PaymentAmount = Session.RemainingAmount > 0 ? Session.RemainingAmount : Session.FinalAmount;
    }

    [RelayCommand]
    private async Task SavePaymentAsync()
    {
        if (Session == null) return;

        var amountDue = Session.RemainingAmount > 0 ? Session.RemainingAmount : Session.FinalAmount;

        if (SelectedPaymentMethod == PaymentMethod.Cash && CashTendered > 0 && CashTendered < amountDue)
        {
            _toast.Warning("Nakit tutarı yetersiz.");
            return;
        }

        try
        {
            var change = SelectedPaymentMethod == PaymentMethod.Cash ? ChangeAmount : 0;
            var payAmount = amountDue;

            Session = await _sessionService.RecordPaymentAsync(_sessionId, payAmount,
                SelectedPaymentMethod, _currentUser.UserId, change, null, Session.RowVersion);

            if (Session.Status == SessionStatus.Paid)
            {
                CompletedPaymentAmount = payAmount;
                PaymentSuccessMessage = $"Ödeme Tamamlandı!\n{payAmount:N2} ₺ kaydedildi.";
                ShowPaymentSuccess = true;
                _toast.Success("Ödeme tamamlandı ve kaydedildi.");
                OnSessionUpdated?.Invoke();
                await Task.Delay(2200);
                ShowPaymentSuccess = false;
                OnSessionClosed?.Invoke();
            }
            else
            {
                PaymentAmount = Session.RemainingAmount;
                _toast.Success("Kısmi ödeme kaydedildi.");
                OnSessionUpdated?.Invoke();
            }
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
    }

    [RelayCommand]
    private async Task CancelSessionAsync()
    {
        if (Session == null) return;
        try
        {
            await _sessionService.CancelSessionAsync(_sessionId, _currentUser.UserId, "İptal edildi", Session.RowVersion);
            _toast.Warning("Oturum iptal edildi.");
            OnSessionClosed?.Invoke();
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
    }
}
