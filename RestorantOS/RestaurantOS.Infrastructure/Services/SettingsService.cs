using Microsoft.EntityFrameworkCore;
using RestaurantOS.Application.DTOs.Settings;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Entities;
using RestaurantOS.Domain.Enums;
using RestaurantOS.Infrastructure.Data;

namespace RestaurantOS.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _context;

    public SettingsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AppSettingsDto> GetAppSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.Settings.ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
        return new AppSettingsDto
        {
            RestaurantName = settings.GetValueOrDefault("RestaurantName", "RestaurantOS"),
            DefaultTaxRate = decimal.TryParse(settings.GetValueOrDefault("DefaultTaxRate"), out var tax) ? tax : 10m,
            AccentColor = settings.GetValueOrDefault("AccentColor", "#4F6EF7")
        };
    }

    public async Task SaveAppSettingsAsync(AppSettingsDto settings, CancellationToken cancellationToken = default)
    {
        await UpsertSettingAsync("RestaurantName", settings.RestaurantName, "Restoran adı", cancellationToken);
        await UpsertSettingAsync("DefaultTaxRate", settings.DefaultTaxRate.ToString(), "Varsayılan KDV", cancellationToken);
        await UpsertSettingAsync("AccentColor", settings.AccentColor, "Tema rengi", cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertSettingAsync(string key, string value, string description, CancellationToken ct)
    {
        var setting = await _context.Settings.FindAsync(new object[] { key }, ct);
        if (setting == null)
            _context.Settings.Add(new Setting { Key = key, Value = value, Description = description });
        else
            setting.Value = value;
    }

    public async Task<IReadOnlyList<TableSettingsDto>> GetTablesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tables
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new TableSettingsDto
            {
                TableId = t.TableId,
                TableNumber = t.TableNumber,
                Name = t.Name,
                Capacity = t.Capacity,
                Section = t.Section,
                IsActive = t.IsActive,
                DisplayOrder = t.DisplayOrder
            }).ToListAsync(cancellationToken);
    }

    public async Task<TableSettingsDto> SaveTableAsync(TableSettingsDto table, CancellationToken cancellationToken = default)
    {
        Table entity;
        if (table.TableId == Guid.Empty)
        {
            entity = new Table();
            _context.Tables.Add(entity);
        }
        else
        {
            entity = await _context.Tables.FindAsync(new object[] { table.TableId }, cancellationToken) ?? new Table();
        }

        entity.TableNumber = table.TableNumber;
        entity.Name = table.Name;
        entity.Capacity = table.Capacity;
        entity.Section = table.Section;
        entity.IsActive = table.IsActive;
        entity.DisplayOrder = table.DisplayOrder;

        await _context.SaveChangesAsync(cancellationToken);
        table.TableId = entity.TableId;
        return table;
    }

    public async Task<IReadOnlyList<UserSettingsDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Select(u => new UserSettingsDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Username = u.Username,
                Role = u.Role,
                IsActive = u.IsActive
            }).ToListAsync(cancellationToken);
    }

    public async Task<UserSettingsDto> SaveUserAsync(UserSettingsDto user, string? password, CancellationToken cancellationToken = default)
    {
        User entity;
        if (user.UserId == Guid.Empty)
        {
            entity = new User();
            _context.Users.Add(entity);
        }
        else
        {
            entity = await _context.Users.FindAsync(new object[] { user.UserId }, cancellationToken) ?? new User();
        }

        entity.FullName = user.FullName;
        entity.Username = user.Username;
        entity.Role = user.Role;
        entity.IsActive = user.IsActive;

        if (!string.IsNullOrEmpty(password))
            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

        await _context.SaveChangesAsync(cancellationToken);
        user.UserId = entity.UserId;
        return user;
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user != null)
        {
            user.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<string> BackupDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RestaurantOS", "Backups", $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        return Task.FromResult($"Yedekleme konumu: {backupPath} (SQL Server BACKUP komutu sunucu tarafında yapılandırılmalıdır)");
    }
}
