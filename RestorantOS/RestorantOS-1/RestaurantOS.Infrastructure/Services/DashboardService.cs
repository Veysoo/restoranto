using Microsoft.EntityFrameworkCore;
using RestaurantOS.Application.DTOs.Dashboard;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Enums;
using RestaurantOS.Infrastructure.Data;

namespace RestaurantOS.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var sevenDaysAgo = today.AddDays(-6);

        var todayPayments = await _context.Payments
            .Where(p => !p.IsVoid && p.PaidAt >= today)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;

        var yesterdayPayments = await _context.Payments
            .Where(p => !p.IsVoid && p.PaidAt >= yesterday && p.PaidAt < today)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;

        var totalTables = await _context.Tables.CountAsync(t => t.IsActive, cancellationToken);
        var openSessions = await _context.Sessions
            .CountAsync(s => s.Status == SessionStatus.Open || s.Status == SessionStatus.Billed, cancellationToken);

        var activeOrders = await _context.OrderItems
            .CountAsync(o => o.Status == OrderItemStatus.Pending || o.Status == OrderItemStatus.Preparing, cancellationToken);

        var paidSessionsToday = await _context.Sessions
            .CountAsync(s => s.Status == SessionStatus.Paid && s.ClosedAt >= today, cancellationToken);

        var avgTicket = paidSessionsToday > 0 ? todayPayments / paidSessionsToday : 0;

        var trend = yesterdayPayments > 0
            ? (double)((todayPayments - yesterdayPayments) / yesterdayPayments * 100)
            : todayPayments > 0 ? 100 : 0;

        var floorOverview = await _context.Tables
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new FloorTableStatusDto
            {
                TableId = t.TableId,
                TableNumber = t.TableNumber,
                Status = t.Sessions.Any(s => s.ClosedAt == null && s.Status != SessionStatus.Cancelled)
                    ? t.Sessions.Where(s => s.ClosedAt == null).OrderByDescending(s => s.OpenedAt).First().Status.ToString()
                    : "Empty"
            }).ToListAsync(cancellationToken);

        var recentTransactions = await _context.Payments
            .Where(p => !p.IsVoid)
            .OrderByDescending(p => p.PaidAt)
            .Take(10)
            .Select(p => new RecentTransactionDto
            {
                PaidAt = p.PaidAt,
                TableName = p.Session.Table.Name,
                Amount = p.Amount,
                Method = p.Method.ToString(),
                ReceivedBy = p.ReceivedByUser.FullName
            }).ToListAsync(cancellationToken);

        var last7Days = await _context.Payments
            .Where(p => !p.IsVoid && p.PaidAt >= sevenDaysAgo)
            .GroupBy(p => p.PaidAt.Date)
            .Select(g => new DailyRevenueDto { Date = g.Key, Revenue = g.Sum(p => p.Amount) })
            .ToListAsync(cancellationToken);

        var hourlyOrders = await _context.OrderItems
            .Where(o => o.CreatedAt >= today && o.Status != OrderItemStatus.Cancelled)
            .GroupBy(o => o.CreatedAt.Hour)
            .Select(g => new HourlyOrderDto { Hour = g.Key, OrderCount = g.Count() })
            .ToListAsync(cancellationToken);

        var todaySoldItems = await _context.OrderItems
            .Include(o => o.MenuItem)
            .Include(o => o.Session)
            .Where(o => o.Status != OrderItemStatus.Cancelled
                && o.Session.Status == SessionStatus.Paid
                && o.Session.ClosedAt >= today)
            .GroupBy(o => o.MenuItem.Name)
            .Select(g => new TodaySoldItemDto
            {
                ItemName = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(t => t.Revenue)
            .Take(15)
            .ToListAsync(cancellationToken);

        var todayItemsSold = todaySoldItems.Sum(t => t.Quantity);

        return new DashboardDto
        {
            TodayRevenue = todayPayments,
            YesterdayRevenue = yesterdayPayments,
            RevenueTrendPercent = trend,
            OpenTables = openSessions,
            TotalTables = totalTables,
            ActiveOrdersCount = activeOrders,
            AverageTicketValue = avgTicket,
            FloorOverview = floorOverview,
            RecentTransactions = recentTransactions,
            Last7DaysRevenue = last7Days,
            HourlyOrdersToday = hourlyOrders,
            TodaySessionCount = paidSessionsToday,
            TodayItemsSold = todayItemsSold,
            TodaySoldItems = todaySoldItems
        };
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var fromUtc = from.Date;
        var toUtc = to.Date.AddDays(1); // exclusive upper bound

        var payments = await _context.Payments
            .Where(p => !p.IsVoid && p.PaidAt >= fromUtc && p.PaidAt < toUtc)
            .Include(p => p.Session)
            .ToListAsync(cancellationToken);

        var totalRevenue = payments.Sum(p => p.Amount);
        var sessions = payments.Select(p => p.SessionId).Distinct().Count();

        var itemsSold = await _context.OrderItems
            .Include(o => o.Session)
            .Where(o => o.Status != OrderItemStatus.Cancelled
                && o.Session.Status == SessionStatus.Paid
                && o.Session.ClosedAt >= fromUtc && o.Session.ClosedAt < toUtc)
            .SumAsync(o => (int?)o.Quantity, cancellationToken) ?? 0;

        var dailyBreakdown = await _context.Payments
            .Where(p => !p.IsVoid && p.PaidAt >= fromUtc && p.PaidAt < toUtc)
            .GroupBy(p => p.PaidAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(p => p.Amount),
                Sessions = g.Select(p => p.SessionId).Distinct().Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        // Fill missing days with zeros
        var allDays = new List<DailyRevenueDetailDto>();
        for (var d = fromUtc; d < toUtc; d = d.AddDays(1))
        {
            var match = dailyBreakdown.FirstOrDefault(x => x.Date == d);
            allDays.Add(new DailyRevenueDetailDto
            {
                Date = d,
                Revenue = match?.Revenue ?? 0,
                Sessions = match?.Sessions ?? 0,
                ItemsSold = 0
            });
        }

        // Top items in range
        var topItems = await _context.OrderItems
            .Include(o => o.MenuItem)
            .Include(o => o.Session)
            .Where(o => o.Status != OrderItemStatus.Cancelled
                && o.Session.Status == SessionStatus.Paid
                && o.Session.ClosedAt >= fromUtc && o.Session.ClosedAt < toUtc)
            .GroupBy(o => o.MenuItem.Name)
            .Select(g => new TodaySoldItemDto
            {
                ItemName = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(t => t.Revenue)
            .Take(20)
            .ToListAsync(cancellationToken);

        return new SalesReportDto
        {
            From = fromUtc,
            To = to.Date,
            TotalRevenue = totalRevenue,
            TotalSessions = sessions,
            TotalItemsSold = itemsSold,
            AverageTicket = sessions > 0 ? Math.Round(totalRevenue / sessions, 2) : 0,
            DailyBreakdown = allDays,
            TopItems = topItems
        };
    }
}
