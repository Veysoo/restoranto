using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOS.Application.DTOs.Settings;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Enums;
using RestaurantOS.WPF.Services;

namespace RestaurantOS.WPF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ToastService _toast;

    [ObservableProperty] private AppSettingsDto _appSettings = new();
    [ObservableProperty] private ObservableCollection<TableSettingsDto> _tables = new();
    [ObservableProperty] private ObservableCollection<UserSettingsDto> _users = new();
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private TableSettingsDto? _editingTable;
    [ObservableProperty] private UserSettingsDto? _editingUser;
    [ObservableProperty] private string _newUserPassword = string.Empty;

    public Array Roles => Enum.GetValues(typeof(UserRole));

    public SettingsViewModel(ISettingsService settingsService, ToastService toast)
    {
        _settingsService = settingsService;
        _toast = toast;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            AppSettings = await _settingsService.GetAppSettingsAsync();
            Tables = new ObservableCollection<TableSettingsDto>(await _settingsService.GetTablesAsync());
            Users = new ObservableCollection<UserSettingsDto>(await _settingsService.GetUsersAsync());
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveAppSettingsAsync()
    {
        await _settingsService.SaveAppSettingsAsync(AppSettings);
        _toast.Success("Ayarlar kaydedildi.");
    }

    [RelayCommand]
    private void AddTable() => EditingTable = new TableSettingsDto { IsActive = true, Capacity = 4, Section = "İç Salon" };

    [RelayCommand]
    private void EditTable(TableSettingsDto? table) => EditingTable = table;

    [RelayCommand]
    private async Task SaveTableAsync()
    {
        if (EditingTable == null) return;
        await _settingsService.SaveTableAsync(EditingTable);
        EditingTable = null;
        await LoadAsync();
        _toast.Success("Masa kaydedildi.");
    }

    [RelayCommand]
    private void AddUser() => EditingUser = new UserSettingsDto { IsActive = true, Role = UserRole.Waiter };

    [RelayCommand]
    private void EditUser(UserSettingsDto? user) => EditingUser = user;

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        if (EditingUser == null) return;
        await _settingsService.SaveUserAsync(EditingUser, string.IsNullOrEmpty(NewUserPassword) ? null : NewUserPassword);
        NewUserPassword = string.Empty;
        EditingUser = null;
        await LoadAsync();
        _toast.Success("Kullanıcı kaydedildi.");
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        var result = await _settingsService.BackupDatabaseAsync();
        _toast.Info(result);
    }
}
