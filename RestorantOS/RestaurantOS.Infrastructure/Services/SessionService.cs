using Microsoft.EntityFrameworkCore;
using RestaurantOS.Application.DTOs.Sessions;
using RestaurantOS.Application.Exceptions;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Application.Mappings;
using RestaurantOS.Application.Services;
using RestaurantOS.Domain.Entities;
using RestaurantOS.Domain.Enums;
using RestaurantOS.Domain.Interfaces;
using RestaurantOS.Infrastructure.Data;

namespace RestaurantOS.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public SessionService(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private async Task<Session> LoadSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await _context.Sessions
            .Include(s => s.Table)
            .Include(s => s.OrderItems).ThenInclude(i => i.MenuItem)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

        if (session == null) throw new BusinessException("Oturum bulunamadı.");
        return session;
    }

    private static void ValidateRowVersion(byte[] entityVersion, byte[] providedVersion)
    {
        if (!entityVersion.SequenceEqual(providedVersion))
            throw new ConcurrencyException();
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
        await _auditService.LogAsync("OpenSession", "Session", session.SessionId.ToString(), userId, cancellationToken: cancellationToken);

        return SessionMapper.ToDetailDto(await LoadSessionAsync(session.SessionId, cancellationToken));
    }

    public async Task<SessionDetailDto?> GetSessionDetailAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.Table)
            .Include(s => s.OrderItems).ThenInclude(i => i.MenuItem)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

        return session == null ? null : SessionMapper.ToDetailDto(session);
    }

    public async Task<SessionDetailDto> UpdateGuestCountAsync(Guid sessionId, int guestCount, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        ValidateRowVersion(session.RowVersion, rowVersion);
        session.GuestCount = guestCount;
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(session);
    }

    public async Task<SessionDetailDto> AddOrderItemAsync(Guid sessionId, Guid menuItemId, int quantity, Guid userId, string? notes, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        ValidateRowVersion(session.RowVersion, rowVersion);

        if (session.Status != SessionStatus.Open)
            throw new BusinessException("Sadece açık oturumlara sipariş eklenebilir.");

        var menuItem = await _context.MenuItems.FindAsync(new object[] { menuItemId }, cancellationToken)
            ?? throw new BusinessException("Menü öğesi bulunamadı.");

        if (!menuItem.IsAvailable) throw new BusinessException("Ürün şu an mevcut değil.");

        var orderItem = new OrderItem
        {
            SessionId = sessionId,
            MenuItemId = menuItemId,
            Quantity = quantity,
            UnitPrice = menuItem.Price,
            LineTotal = SessionCalculator.CalculateLineTotal(quantity, menuItem.Price, 0),
            AddedByUserId = userId,
            Notes = notes,
            Status = OrderItemStatus.Pending
        };

        session.OrderItems.Add(orderItem);
        SessionCalculator.Recalculate(session);
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(await LoadSessionAsync(sessionId, cancellationToken));
    }

    public async Task<SessionDetailDto> UpdateOrderItemQuantityAsync(Guid orderItemId, int quantity, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var item = await _context.OrderItems
            .Include(i => i.Session).ThenInclude(s => s.OrderItems).ThenInclude(o => o.MenuItem)
            .Include(i => i.Session).ThenInclude(s => s.Payments)
            .Include(i => i.Session).ThenInclude(s => s.Table)
            .FirstOrDefaultAsync(i => i.OrderItemId == orderItemId, cancellationToken)
            ?? throw new BusinessException("Sipariş bulunamadı.");

        ValidateRowVersion(item.RowVersion, rowVersion);
        if (item.Status == OrderItemStatus.Cancelled) throw new BusinessException("İptal edilmiş ürün güncellenemez.");

        item.Quantity = quantity;
        item.LineTotal = SessionCalculator.CalculateLineTotal(quantity, item.UnitPrice, item.Discount);
        SessionCalculator.Recalculate(item.Session);
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(item.Session);
    }

    public async Task<SessionDetailDto> RemoveOrderItemAsync(Guid orderItemId, Guid userId, string? reason, bool adminOverride, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var item = await _context.OrderItems
            .Include(i => i.Session).ThenInclude(s => s.OrderItems).ThenInclude(o => o.MenuItem)
            .Include(i => i.Session).ThenInclude(s => s.Payments)
            .Include(i => i.Session).ThenInclude(s => s.Table)
            .FirstOrDefaultAsync(i => i.OrderItemId == orderItemId, cancellationToken)
            ?? throw new BusinessException("Sipariş bulunamadı.");

        ValidateRowVersion(item.RowVersion, rowVersion);

        if (item.Status >= OrderItemStatus.Preparing && !adminOverride)
            throw new BusinessException("Hazırlanan ürünler admin onayı olmadan silinemez.");

        item.Status = OrderItemStatus.Cancelled;
        await _auditService.LogAsync("CancelOrderItem", "OrderItem", orderItemId.ToString(), userId,
            newValues: $"{{\"reason\":\"{reason}\"}}", cancellationToken: cancellationToken);

        SessionCalculator.Recalculate(item.Session);
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(item.Session);
    }

    public async Task<SessionDetailDto> UpdateOrderItemNotesAsync(Guid orderItemId, string? notes, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var item = await _context.OrderItems
            .Include(i => i.Session).ThenInclude(s => s.OrderItems).ThenInclude(o => o.MenuItem)
            .Include(i => i.Session).ThenInclude(s => s.Payments)
            .Include(i => i.Session).ThenInclude(s => s.Table)
            .FirstOrDefaultAsync(i => i.OrderItemId == orderItemId, cancellationToken)
            ?? throw new BusinessException("Sipariş bulunamadı.");

        ValidateRowVersion(item.RowVersion, rowVersion);
        item.Notes = notes;
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(item.Session);
    }

    public async Task<SessionDetailDto> ApplyDiscountAsync(Guid sessionId, decimal discountAmount, bool isPercentage, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        ValidateRowVersion(session.RowVersion, rowVersion);

        session.DiscountAmount = isPercentage
            ? Math.Round(session.TotalAmount * discountAmount / 100m, 2)
            : discountAmount;

        SessionCalculator.Recalculate(session);
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(session);
    }

    public async Task<SessionDetailDto> RequestBillAsync(Guid sessionId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        ValidateRowVersion(session.RowVersion, rowVersion);

        if (session.Status != SessionStatus.Open)
            throw new BusinessException("Hesap sadece açık oturumlar için kesilebilir.");

        session.Status = SessionStatus.Billed;
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(session);
    }

    public async Task<SessionDetailDto> RecordPaymentAsync(Guid sessionId, decimal amount, PaymentMethod method, Guid userId, decimal changeGiven, string? referenceNo, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        ValidateRowVersion(session.RowVersion, rowVersion);

        if (session.Status != SessionStatus.Billed && session.Status != SessionStatus.Open)
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

        session.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        session = await LoadSessionAsync(sessionId, cancellationToken);
        var paidTotal = session.Payments.Where(p => !p.IsVoid).Sum(p => p.Amount);

        if (paidTotal >= session.FinalAmount)
        {
            session.Status = SessionStatus.Paid;
            session.ClosedAt = DateTime.UtcNow;
            session.ClosedByUserId = userId;
            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync("CloseSession", "Session", sessionId.ToString(), userId, cancellationToken: cancellationToken);
        }

        return SessionMapper.ToDetailDto(await LoadSessionAsync(sessionId, cancellationToken));
    }

    public async Task CancelSessionAsync(Guid sessionId, Guid userId, string reason, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException("İptal nedeni zorunludur.");

        var session = await LoadSessionAsync(sessionId, cancellationToken);
        ValidateRowVersion(session.RowVersion, rowVersion);

        session.Status = SessionStatus.Cancelled;
        session.ClosedAt = DateTime.UtcNow;
        session.ClosedByUserId = userId;
        session.Notes = reason;

        foreach (var item in session.OrderItems.Where(i => i.Status != OrderItemStatus.Cancelled))
            item.Status = OrderItemStatus.Cancelled;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("CancelSession", "Session", sessionId.ToString(), userId,
            newValues: $"{{\"reason\":\"{reason}\"}}", cancellationToken: cancellationToken);
    }

    public async Task<SessionDetailDto> MoveSessionAsync(Guid sessionId, Guid targetTableId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        ValidateRowVersion(session.RowVersion, rowVersion);

        var targetOccupied = await _context.Sessions
            .AnyAsync(s => s.TableId == targetTableId && s.ClosedAt == null && s.Status != SessionStatus.Cancelled, cancellationToken);

        if (targetOccupied) throw new BusinessException("Hedef masa dolu.");

        session.TableId = targetTableId;
        await _context.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetailDto(await LoadSessionAsync(sessionId, cancellationToken));
    }
}
