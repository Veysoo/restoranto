using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using RestaurantOS.Application.DTOs.Dashboard;
using RestaurantOS.Application.Interfaces;
using SkiaSharp;

namespace RestaurantOS.WPF.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private double _todayRevenue;
    [ObservableProperty] private double _revenueTrend;
    [ObservableProperty] private int _openTables;
    [ObservableProperty] private int _totalTables;
    [ObservableProperty] private int _activeOrders;
    [ObservableProperty] private double _averageTicket;
    [ObservableProperty] private List<FloorTableStatusDto> _floorOverview = new();
    [ObservableProperty] private List<RecentTransactionDto> _recentTransactions = new();
    [ObservableProperty] private List<TodaySoldItemDto> _todaySoldItems = new();
    [ObservableProperty] private int _todaySessionCount;
    [ObservableProperty] private int _todayItemsSold;
    public bool HasNoTodaySales => TodaySoldItems.Count == 0;

    public ISeries[] RevenueSeries { get; private set; } = Array.Empty<ISeries>();
    public Axis[] RevenueXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] RevenueYAxes { get; private set; } = Array.Empty<Axis>();

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            var data = await _dashboardService.GetDashboardAsync();
            TodayRevenue = (double)data.TodayRevenue;
            RevenueTrend = data.RevenueTrendPercent;
            OpenTables = data.OpenTables;
            TotalTables = data.TotalTables;
            ActiveOrders = data.ActiveOrdersCount;
            AverageTicket = (double)data.AverageTicketValue;
            FloorOverview = data.FloorOverview.ToList();
            RecentTransactions = data.RecentTransactions.ToList();
            TodaySoldItems = data.TodaySoldItems.ToList();
            TodaySessionCount = data.TodaySessionCount;
            TodayItemsSold = data.TodayItemsSold;
            OnPropertyChanged(nameof(HasNoTodaySales));

            var labels = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-6 + i).ToString("dd MMM")).ToArray();
            var values = new List<double>();
            for (var i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(-6 + i);
                var rev = data.Last7DaysRevenue.FirstOrDefault(d => d.Date.Date == date)?.Revenue ?? 0;
                values.Add((double)rev);
            }

            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = values,
                    Fill = new SolidColorPaint(SKColor.Parse("#4F6EF7")),
                    MaxBarWidth = 40
                }
            };
            RevenueXAxes = new[] { new Axis { Labels = labels, LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")), SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2D3148")) } };
            RevenueYAxes = new[] { new Axis { LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")), SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2D3148")) } };
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
