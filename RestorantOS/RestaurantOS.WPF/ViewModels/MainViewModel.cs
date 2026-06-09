using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.WPF.Services;

namespace RestaurantOS.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly NavigationService _navigation;
    private readonly CurrentUserService _currentUser;
    private readonly IDashboardService _dashboardService;
    private readonly System.Windows.Threading.DispatcherTimer _clockTimer;

    [ObservableProperty] private object? _currentViewModel;
    [ObservableProperty] private string _currentTime = DateTime.Now.ToString("HH:mm:ss");
    [ObservableProperty] private string _userDisplay = string.Empty;
    [ObservableProperty] private string _userInitials = "U";
    [ObservableProperty] private int _openTablesCount;
    [ObservableProperty] private string _todayRevenue = "₺0,00";
    [ObservableProperty] private bool _isSidebarExpanded;

    public MainViewModel(NavigationService navigation, CurrentUserService currentUser, IDashboardService dashboardService)
    {
        _navigation = navigation;
        _currentUser = currentUser;
        _dashboardService = dashboardService;
        UserDisplay = currentUser.FullName;
        UserInitials = string.Concat(currentUser.FullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n[0])
            .Take(2)).ToUpper();
        if (string.IsNullOrEmpty(UserInitials)) UserInitials = "U";

        _navigation.Navigated += OnNavigated;
        _clockTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += async (_, _) =>
        {
            CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            await RefreshQuickStatsAsync();
        };
        _clockTimer.Start();

        NavigateDashboard();
        _ = RefreshQuickStatsAsync();
    }

    private void OnNavigated(Type vmType)
    {
        CurrentViewModel = _navigation.CurrentViewModel;
    }

    [RelayCommand] private void NavigateDashboard() => _navigation.NavigateTo<DashboardViewModel>();
    [RelayCommand] private void NavigateFloor() => _navigation.NavigateTo<FloorViewModel>();
    [RelayCommand] private void NavigateOrders() => _navigation.NavigateTo<OrdersViewModel>();
    [RelayCommand] private void NavigateMenu() => _navigation.NavigateTo<MenuViewModel>();
    [RelayCommand] private void NavigateReports() => _navigation.NavigateTo<ReportsViewModel>();
    [RelayCommand] private void NavigateSettings() => _navigation.NavigateTo<SettingsViewModel>();

    private async Task RefreshQuickStatsAsync()
    {
        try
        {
            var dash = await _dashboardService.GetDashboardAsync();
            OpenTablesCount = dash.OpenTables;
            TodayRevenue = dash.TodayRevenue.ToString("₺#,##0.00", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        }
        catch { /* silent */ }
    }
}
