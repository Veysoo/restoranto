using Microsoft.EntityFrameworkCore;
using RestaurantOS.Application.DTOs.Sessions;
using RestaurantOS.Application.Exceptions;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Application.Mappings;
using RestaurantOS.Application.Services;
using RestaurantOS.Domain.Entities;
using RestaurantOS.Domain.Enums;
using RestaurantOS.Infrastructure.Data;

namespace RestaurantOS.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly AppDbContext _context;

    public SessionService(AppDbContext context)
    {
        _context = context;
    }

    private async Task<Session> LoadSessionAsync(Guid sessionId, CancellationToken ct)
    {
        return await _context.Sessions
            .Include(s => s.Table)
            .Include(s => s.OrderItems).ThenInclude(i => i.MenuItem)
            .Include(s => s.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct)
            ?? throw new BusinessException("Oturum bulunamadı.");
    }

    private async Task<Session> TrackSessionAsync(Guid sessionId, CancellationToken ct)
    {
        return await _context.Sessions
            .Include(s => s.Table)
            .Include(s => s.OrderItems).ThenInclude(i => i.MenuItem)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct)
            ?? throw new BusinessException("Oturum bulunamadı.");
    }

    public async Task<SessionDetailDto> OpenSessionAsync(Guid tableId, int guestCount, Guid userId, CancellationToken cancellationToken = default)
    {
        var table = await _context.Tables.FindAsync(new object[] { tableId }, cancellationToken)
            ?? throw new BusinessException("Masa bulunamadı.");

        var existing = await _context.Sessions
            .AnyAsync(s => s.TableId == tableId && s.ClosedAt == null && s.Status != SessionStatus.Cancelled, cancellationToken);

        if (existing) throw new BusinessException("Masa zaten dolu.");

        var session = new Session
        {
            TableId = tableId,
            GuestCount = guestCount,
            OpenedByUserId = userId,
            Status = SessionStatus.Open
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return SessionMapper.ToDetailDto(await LoadSessionAsync(session.SessionId, cancellationToken));
    }

    public async Task<SessionDetailDto?> GetSessionDetailAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.Table)
            .Include(s => s.OrderItems).ThenInclude(i => i.MenuItem)
            .Include(s => s.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

        return session == null ? null : SessionMapper.ToDetailDto(session);
    }

    public async Task<SessionDetailDto> UpdateGuestCountAsync(Guid sessionId, int guestCount, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var session = await TrackSessionAsync(sessionId, cancellationToken);
        session.GuestCount = guestCount;
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(await LoadSessionAsync(sessionId, cancellationToken));
    }

    public async Task<SessionDetailDto> AddOrderItemAsync(Guid sessionId, Guid menuItemId, int quantity, Guid userId, string? notes, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        // Validate session status
        var sessionCheck = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId)
            .Select(s => new { s.Status })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException("Oturum bulunamadı.");

        if (sessionCheck.Status != SessionStatus.Open)
            throw new BusinessException("Sadece açık oturumlara sipariş eklenebilir.");

        // Validate menu item (tracked for FK reference)
        var menuItem = await _context.MenuItems
            .FirstOrDefaultAsync(m => m.MenuItemId == menuItemId, cancellationToken)
            ?? throw new BusinessException("Menü öğesi bulunamadı.");

        if (!menuItem.IsAvailable) throw new BusinessException("Ürün şu an mevcut değil.");

        var lineTotal = SessionCalculator.CalculateLineTotal(quantity, menuItem.Price, 0);

        // Insert the order item directly (no Session entity modification)
        var orderItem = new OrderItem
        {
            SessionId = sessionId,
            MenuItemId = menuItemId,
            Quantity = quantity,
            UnitPrice = menuItem.Price,
            LineTotal = lineTotal,
            AddedByUserId = userId,
            Notes = notes,
            Status = OrderItemStatus.Pending
        };

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync(cancellationToken);
            await RecalculateSessionTotalsAsync(sessionId, cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });

        return SessionMapper.ToDetailDto(await LoadSessionAsync(sessionId, cancellationToken));
    }

    private async Task RecalculateSessionTotalsAsync(Guid sessionId, CancellationToken ct)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE Sessions SET
                TotalAmount = (SELECT ISNULL(SUM(o.LineTotal), 0) FROM OrderItems o WHERE o.SessionId = {sessionId} AND o.Status != 'Cancelled'),
                TaxAmount = (SELECT ISNULL(SUM(ROUND((o.LineTotal - o.Discount) * m.TaxRate / 100.0, 2)), 0) FROM OrderItems o INNER JOIN MenuItems m ON o.MenuItemId = m.MenuItemId WHERE o.SessionId = {sessionId} AND o.Status != 'Cancelled'),
                FinalAmount = (SELECT ISNULL(SUM(o.LineTotal), 0) FROM OrderItems o WHERE o.SessionId = {sessionId} AND o.Status != 'Cancelled')
                            - DiscountAmount
                            + (SELECT ISNULL(SUM(ROUND((o.LineTotal - o.Discount) * m.TaxRate / 100.0, 2)), 0) FROM OrderItems o INNER JOIN MenuItems m ON o.MenuItemId = m.MenuItemId WHERE o.SessionId = {sessionId} AND o.Status != 'Cancelled')
             WHERE SessionId = {sessionId}", ct);
    }

    public async Task<SessionDetailDto> UpdateOrderItemQuantityAsync(Guid orderItemId, int quantity, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var item = await _context.OrderItems
            .FirstOrDefaultAsync(i => i.OrderItemId == orderItemId, cancellationToken)
            ?? throw new BusinessException("Sipariş bulunamadı.");

        if (item.Status == OrderItemStatus.Cancelled) throw new BusinessException("İptal edilmiş ürün güncellenemez.");

        var qSessionId = item.SessionId;
        var qStrategy = _context.Database.CreateExecutionStrategy();
        await qStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            item.Quantity = quantity;
            item.LineTotal = SessionCalculator.CalculateLineTotal(quantity, item.UnitPrice, item.Discount);
            await _context.SaveChangesAsync(cancellationToken);
            await RecalculateSessionTotalsAsync(qSessionId, cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });

        return SessionMapper.ToDetailDto(await LoadSessionAsync(qSessionId, cancellationToken));
    }

    public async Task<SessionDetailDto> RemoveOrderItemAsync(Guid orderItemId, Guid userId, string? reason, bool adminOverride, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var item = await _context.OrderItems
            .FirstOrDefaultAsync(i => i.OrderItemId == orderItemId, cancellationToken)
            ?? throw new BusinessException("Sipariş bulunamadı.");

        if (item.Status >= OrderItemStatus.Preparing && !adminOverride)
            throw new BusinessException("Hazırlanan ürünler silinemez.");

        var sessionId2 = item.SessionId;
        var strategy2 = _context.Database.CreateExecutionStrategy();
        await strategy2.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            item.Status = OrderItemStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);
            await RecalculateSessionTotalsAsync(sessionId2, cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });

        return SessionMapper.ToDetailDto(await LoadSessionAsync(sessionId2, cancellationToken));
    }

    public async Task<SessionDetailDto> UpdateOrderItemNotesAsync(Guid orderItemId, string? notes, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var item = await _context.OrderItems
            .Include(i => i.Session).ThenInclude(s => s.OrderItems).ThenInclude(o => o.MenuItem)
            .Include(i => i.Session).ThenInclude(s => s.Payments)
            .Include(i => i.Session).ThenInclude(s => s.Table)
            .FirstOrDefaultAsync(i => i.OrderItemId == orderItemId, cancellationToken)
            ?? throw new BusinessException("Sipariş bulunamadı.");

        item.Notes = notes;
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(item.Session);
    }

    public async Task<SessionDetailDto> ApplyDiscountAsync(Guid sessionId, decimal discountAmount, bool isPercentage, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var session = await TrackSessionAsync(sessionId, cancellationToken);
        session.DiscountAmount = isPercentage
            ? Math.Round(session.TotalAmount * discountAmount / 100m, 2)
            : discountAmount;

        SessionCalculator.Recalculate(session);
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(await LoadSessionAsync(sessionId, cancellationToken));
    }

    public async Task<SessionDetailDto> RequestBillAsync(Guid sessionId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .AsNoTracking()
            .Include(s => s.OrderItems)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken)
            ?? throw new BusinessException("Oturum bulunamadı.");

        if (session.Status != SessionStatus.Open)
            throw new BusinessException("Hesap sadece açık oturumlar için kesilebilir.");

        if (!session.OrderItems.Any(i => i.Status != OrderItemStatus.Cancelled))
            throw new BusinessException("Önce sipariş ekleyin.");

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Sessions SET Status = 'Billed' WHERE SessionId = {sessionId}",
            cancellationToken);

        return SessionMapper.ToDetailDto(await LoadSessionAsync(sessionId, cancellationToken));
    }

    public async Task<SessionDetailDto> RecordPaymentAsync(Guid sessionId, decimal amount, PaymentMethod method, Guid userId, decimal changeGiven, string? referenceNo, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var sessionCheck = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId)
            .Select(s => new { s.Status, s.FinalAmount })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException("Oturum bulunamadı.");

        if (sessionCheck.Status != SessionStatus.Billed && sessionCheck.Status != SessionStatus.Open)
            throw new BusinessException("Bu oturum için ödeme alınamaz.");

        var payment = new Payment
        {
            SessionId = sessionId,
            Amount = amount,
            Method = method,
            ReceivedByUserId = userId,
            ChangeGiven = changeGiven,
            ReferenceNo = referenceNo
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        // Check if fully paid and close the session with direct SQL
        var paidTotal = await _context.Payments
            .Where(p => p.SessionId == sessionId && !p.IsVoid)
            .SumAsync(p => p.Amount, cancellationToken);

        if (paidTotal >= sessionCheck.FinalAmount)
        {
            var now = DateTime.UtcNow;
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Sessions SET Status = 'Paid', ClosedAt = {now}, ClosedByUserId = {userId} WHERE SessionId = {sessionId}",
                cancellationToken);
        }

        return SessionMapper.ToDetailDto(await LoadSessionAsync(sessionId, cancellationToken));
    }

    public async Task CancelSessionAsync(Guid sessionId, Guid userId, string reason, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException("İptal nedeni zorunludur.");

        var now = DateTime.UtcNow;
        var safeReason = reason.Replace("'", "''");

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Sessions SET Status = 'Cancelled', ClosedAt = {now}, ClosedByUserId = {userId}, Notes = {reason} WHERE SessionId = {sessionId}",
            cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE OrderItems SET Status = 'Cancelled' WHERE SessionId = {sessionId} AND Status != 'Cancelled'",
            cancellationToken);
    }

    public async Task<SessionDetailDto> MoveSessionAsync(Guid sessionId, Guid targetTableId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var session = await TrackSessionAsync(sessionId, cancellationToken);

        var targetOccupied = await _context.Sessions
            .AnyAsync(s => s.TableId == targetTableId && s.ClosedAt == null && s.Status != SessionStatus.Cancelled, cancellationToken);

        if (targetOccupied) throw new BusinessException("Hedef masa dolu.");

        session.TableId = targetTableId;
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(await LoadSessionAsync(sessionId, cancellationToken));
    }
}
