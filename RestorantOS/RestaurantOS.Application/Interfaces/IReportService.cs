using RestaurantOS.Application.DTOs.Reports;

namespace RestaurantOS.Application.Interfaces;

public interface IReportService
{
    Task<ReportSummaryDto> GetReportAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<byte[]> ExportToExcelAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
