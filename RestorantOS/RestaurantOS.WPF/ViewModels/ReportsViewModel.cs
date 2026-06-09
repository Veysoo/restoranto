using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using RestaurantOS.Application.DTOs.Reports;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.WPF.Services;
using SkiaSharp;

namespace RestaurantOS.WPF.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly ToastService _toast;

    [ObservableProperty] private DateTime _startDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _endDate = DateTime.Today;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ReportSummaryDto? _report;
    [ObservableProperty] private ObservableCollection<DailyRevenueRowDto> _dailyRevenue = new();
    [ObservableProperty] private ObservableCollection<TopItemDto> _topItems = new();
    [ObservableProperty] private ObservableCollection<WaiterPerformanceDto> _waiterPerformance = new();

    public ISeries[] PaymentSeries { get; private set; } = Array.Empty<ISeries>();

    public ReportsViewModel(IReportService reportService, ToastService toast)
    {
        _reportService = reportService;
        _toast = toast;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Report = await _reportService.GetReportAsync(StartDate, EndDate);
            DailyRevenue = new ObservableCollection<DailyRevenueRowDto>(Report.DailyRevenue);
            TopItems = new ObservableCollection<TopItemDto>(Report.TopItemsByRevenue);
            WaiterPerformance = new ObservableCollection<WaiterPerformanceDto>(Report.WaiterPerformance);

            PaymentSeries = Report.PaymentBreakdown.Select(p => new PieSeries<double>
            {
                Values = new[] { (double)p.Amount },
                Name = p.Method,
                Fill = new SolidColorPaint(SKColor.Parse(GetColorForMethod(p.Method)))
            }).ToArray();
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        try
        {
            var bytes = await _reportService.ExportToExcelAsync(StartDate, EndDate);
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "RestaurantOS", $"Rapor_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes);
            _toast.Success($"Excel kaydedildi: {path}");
        }
        catch (Exception ex) { _toast.Error(ex.Message); }
    }

    private static string GetColorForMethod(string method) => method switch
    {
        "Cash" => "#10B981",
        "CreditCard" => "#4F6EF7",
        "DebitCard" => "#7C3AED",
        "Transfer" => "#F59E0B",
        _ => "#94A3B8"
    };
}
