using Microsoft.EntityFrameworkCore;
using RestaurantOS.Application.DTOs.Floor;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Enums;
using RestaurantOS.Infrastructure.Data;

namespace RestaurantOS.Infrastructure.Services;

public class FloorService : IFloorService
{
    private readonly AppDbContext _context;

    public FloorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TableCardDto>> GetTablesAsync(string? section = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Tables
            .Where(t => t.IsActive)
            .Include(t => t.Sessions.Where(s => s.ClosedAt == null && s.Status != SessionStatus.Cancelled))
                .ThenInclude(s => s.OpenedByUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(section) && section != "Tümü")
            query = query.Where(t => t.Section == section);

        var tables = await query
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.TableNumber)
            .ToListAsync(cancellationToken);

        return tables.Select(t =>
        {
            var session = t.Sessions.OrderByDescending(s => s.OpenedAt).FirstOrDefault();
            var status = TableDisplayStatus.Empty;
            if (session != null)
            {
                status = session.Status switch
                {
                    SessionStatus.Open => TableDisplayStatus.Occupied,
                    SessionStatus.Billed => TableDisplayStatus.Billed,
                    SessionStatus.Paid => TableDisplayStatus.Paid,
                    _ => TableDisplayStatus.Empty
                };
            }

            var initials = session?.OpenedByUser?.FullName?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n[0])
                .Take(2);
            var waiterInitials = initials != null ? string.Concat(initials).ToUpper() : null;

            return new TableCardDto
            {
                TableId = t.TableId,
                TableNumber = t.TableNumber,
                Name = t.Name,
                Capacity = t.Capacity,
                Section = t.Section,
                Status = status,
                SessionId = session?.SessionId,
                GuestCount = session?.GuestCount ?? 0,
                OpenedAt = session?.OpenedAt,
                TotalAmount = session?.FinalAmount ?? 0,
                WaiterInitials = waiterInitials,
                RowVersion = session?.RowVersion
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<string>> GetSectionsAsync(CancellationToken cancellationToken = default)
    {
        var sections = await _context.Tables
            .Where(t => t.IsActive)
            .Select(t => t.Section)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);

        return sections;
    }
}
