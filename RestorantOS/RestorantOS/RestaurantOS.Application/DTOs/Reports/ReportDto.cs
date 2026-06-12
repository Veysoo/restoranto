namespace RestaurantOS.Application.DTOs.Reports;

public class ReportSummaryDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalSessions { get; set; }
    public decimal AverageTicket { get; set; }
    public List<DailyRevenueRowDto> DailyRevenue { get; set; } = new();
    public List<TopItemDto> TopItemsByRevenue { get; set; } = new();
    public List<TopItemDto> TopItemsByQuantity { get; set; } = new();
    public List<WaiterPerformanceDto> WaiterPerformance { get; set; } = new();
    public List<PaymentMethodBreakdownDto> PaymentBreakdown { get; set; } = new();
}

public class DailyRevenueRowDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int SessionCount { get; set; }
}

public class TopItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class WaiterPerformanceDto
{
    public string WaiterName { get; set; } = string.Empty;
    public int SessionsOpened { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageTicket { get; set; }
}

public class PaymentMethodBreakdownDto
{
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
}
