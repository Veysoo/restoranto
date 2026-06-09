using Microsoft.EntityFrameworkCore;
using RestaurantOS.Application.DTOs.Orders;
using RestaurantOS.Application.Exceptions;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Enums;
using RestaurantOS.Infrastructure.Data;

namespace RestaurantOS.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OrderKanbanDto>> GetKanbanOrdersAsync(string? section = null, Guid? waiterId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderItems
            .Include(o => o.Session).ThenInclude(s => s.Table)
            .Include(o => o.MenuItem)
            .Include(o => o.AddedByUser)
            .Where(o => o.Session.ClosedAt == null && o.Session.Status != SessionStatus.Cancelled)
            .AsQueryable();

        if (!string.IsNullOrEmpty(section) && section != "Tümü")
            query = query.Where(o => o.Session.Table.Section == section);

        if (waiterId.HasValue)
            query = query.Where(o => o.AddedByUserId == waiterId);

        return await query
            .OrderBy(o => o.CreatedAt)
            .Select(o => new OrderKanbanDto
            {
                OrderItemId = o.OrderItemId,
                SessionId = o.SessionId,
                TableNumber = o.Session.Table.TableNumber,
                TableName = o.Session.Table.Name,
                Section = o.Session.Table.Section,
                ItemName = o.MenuItem.Name,
                Quantity = o.Quantity,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                WaiterName = o.AddedByUser.FullName,
                Notes = o.Notes,
                RowVersion = o.RowVersion
            }).ToListAsync(cancellationToken);
    }

    public async Task UpdateOrderStatusAsync(Guid orderItemId, OrderItemStatus newStatus, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var item = await _context.OrderItems.FindAsync(new object[] { orderItemId }, cancellationToken)
            ?? throw new BusinessException("Sipariş bulunamadı.");

        if (!item.RowVersion.SequenceEqual(rowVersion))
            throw new ConcurrencyException();

        item.Status = newStatus;
        if (newStatus == OrderItemStatus.Preparing && item.SentToKitchenAt == null)
            item.SentToKitchenAt = DateTime.UtcNow;
        if (newStatus == OrderItemStatus.Served)
            item.ServedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
