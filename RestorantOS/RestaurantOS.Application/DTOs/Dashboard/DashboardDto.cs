namespace RestaurantOS.Application.DTOs.Dashboard;

public class SalesReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalSessions { get; set; }
    public int TotalItemsSold { get; set; }
    public decimal AverageTicket { get; set; }
    public List<DailyRevenueDetailDto> DailyBreakdown { get; set; } = new();
    public List<TodaySoldItemDto> TopItems { get; set; } = new();
}

public class DailyRevenueDetailDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int Sessions { get; set; }
    public int ItemsSold { get; set; }
}

public class DashboardDto
{
    public decimal TodayRevenue { get; set; }
    public decimal YesterdayRevenue { get; set; }
    public double RevenueTrendPercent { get; set; }
    public int OpenTables { get; set; }
    public int TotalTables { get; set; }
    public int ActiveOrdersCount { get; set; }
    public decimal AverageTicketValue { get; set; }
    public List<FloorTableStatusDto> FloorOverview { get; set; } = new();
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    public List<DailyRevenueDto> Last7DaysRevenue { get; set; } = new();
    public List<HourlyOrderDto> HourlyOrdersToday { get; set; } = new();
    public int TodaySessionCount { get; set; }
    public int TodayItemsSold { get; set; }
    public List<TodaySoldItemDto> TodaySoldItems { get; set; } = new();
}

public class TodaySoldItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class FloorTableStatusDto
{
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RecentTransactionDto
{
    public DateTime PaidAt { get; set; }
    public string TableName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
}

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
}

public class HourlyOrderDto
{
    public int Hour { get; set; }
    public int OrderCount { get; set; }
}
