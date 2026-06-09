using RestaurantOS.Application.DTOs.Dashboard;

namespace RestaurantOS.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
