using RestaurantOS.Application.DTOs.Settings;
using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Application.Interfaces;

public interface ISettingsService
{
    Task<AppSettingsDto> GetAppSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveAppSettingsAsync(AppSettingsDto settings, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TableSettingsDto>> GetTablesAsync(CancellationToken cancellationToken = default);
    Task<TableSettingsDto> SaveTableAsync(TableSettingsDto table, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSettingsDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserSettingsDto> SaveUserAsync(UserSettingsDto user, string? password, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<string> BackupDatabaseAsync(CancellationToken cancellationToken = default);
}
