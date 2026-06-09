using RestaurantOS.Application.DTOs.Floor;

namespace RestaurantOS.Application.Interfaces;

public interface IFloorService
{
    Task<IReadOnlyList<TableCardDto>> GetTablesAsync(string? section = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSectionsAsync(CancellationToken cancellationToken = default);
}
