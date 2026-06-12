using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using RestaurantOS.Application.DTOs.Reports;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Enums;
using RestaurantOS.Infrastructure.Data;

namespace RestaurantOS.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReportSummaryDto> GetReportAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        var payments = await _context.Payments
            .Where(p => !p.IsVoid && p.PaidAt >= start && p.PaidAt < end)
            .Include(p => p.Session)
            .ToListAsync(cancellationToken);

        var sessions = await _context.Sessions
            .Where(s => s.Status == SessionStatus.Paid && s.ClosedAt >= start && s.ClosedAt < end)
            .ToListAsync(cancellationToken);

        var totalRevenue = payments.Sum(p => p.Amount);
        var sessionCount = sessions.Count;
        var avgTicket = sessionCount > 0 ? totalRevenue / sessionCount : 0;

        var dailyRevenue = payments
            .GroupBy(p => p.PaidAt.Date)
            .Select(g => new DailyRevenueRowDto
            {
                Date = g.Key,
                Revenue = g.Sum(p => p.Amount),
                SessionCount = g.Select(p => p.SessionId).Distinct().Count()
            }).OrderBy(d => d.Date).ToList();

        var orderItems = await _context.OrderItems
            .Include(o => o.MenuItem)
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != OrderItemStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var topByRevenue = orderItems
            .GroupBy(o => o.MenuItem.Name)
            .Select(g => new TopItemDto { ItemName = g.Key, Quantity = g.Sum(o => o.Quantity), Revenue = g.Sum(o => o.LineTotal) })
            .OrderByDescending(t => t.Revenue).Take(10).ToList();

        var topByQty = orderItems
            .GroupBy(o => o.MenuItem.Name)
            .Select(g => new TopItemDto { ItemName = g.Key, Quantity = g.Sum(o => o.Quantity), Revenue = g.Sum(o => o.LineTotal) })
            .OrderByDescending(t => t.Quantity).Take(10).ToList();

        var waiterPerf = await _context.Sessions
            .Where(s => s.Status == SessionStatus.Paid && s.ClosedAt >= start && s.ClosedAt < end)
            .Include(s => s.OpenedByUser)
            .GroupBy(s => s.OpenedByUser.FullName)
            .Select(g => new WaiterPerformanceDto
            {
                WaiterName = g.Key,
                SessionsOpened = g.Count(),
                TotalRevenue = g.Sum(s => s.FinalAmount),
                AverageTicket = g.Average(s => s.FinalAmount)
            }).ToListAsync(cancellationToken);

        var paymentBreakdown = payments
            .GroupBy(p => p.Method)
            .Select(g => new PaymentMethodBreakdownDto
            {
                Method = g.Key.ToString(),
                Amount = g.Sum(p => p.Amount),
                Percentage = totalRevenue > 0 ? (double)(g.Sum(p => p.Amount) / totalRevenue * 100) : 0
            }).ToList();

        return new ReportSummaryDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRevenue = totalRevenue,
            TotalSessions = sessionCount,
            AverageTicket = avgTicket,
            DailyRevenue = dailyRevenue,
            TopItemsByRevenue = topByRevenue,
            TopItemsByQuantity = topByQty,
            WaiterPerformance = waiterPerf,
            PaymentBreakdown = paymentBreakdown
        };
    }

    public async Task<byte[]> ExportToExcelAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var report = await GetReportAsync(startDate, endDate, cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Rapor");
        ws.Cell(1, 1).Value = "RestaurantOS Rapor";
        ws.Cell(2, 1).Value = $"Dönem: {startDate:d} - {endDate:d}";
        ws.Cell(4, 1).Value = "Toplam Gelir";
        ws.Cell(4, 2).Value = report.TotalRevenue;
        ws.Cell(5, 1).Value = "Oturum Sayısı";
        ws.Cell(5, 2).Value = report.TotalSessions;

        var row = 7;
        ws.Cell(row, 1).Value = "Tarih";
        ws.Cell(row, 2).Value = "Gelir";
        ws.Cell(row, 3).Value = "Oturum";
        row++;
        foreach (var d in report.DailyRevenue)
        {
            ws.Cell(row, 1).Value = d.Date;
            ws.Cell(row, 2).Value = d.Revenue;
            ws.Cell(row, 3).Value = d.SessionCount;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
