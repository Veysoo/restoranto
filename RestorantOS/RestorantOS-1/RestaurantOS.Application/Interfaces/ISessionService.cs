using RestaurantOS.Application.DTOs.Sessions;
using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Application.Interfaces;

public interface ISessionService
{
    Task<SessionDetailDto> OpenSessionAsync(Guid tableId, int guestCount, Guid userId, CancellationToken cancellationToken = default);
    Task<SessionDetailDto?> GetSessionDetailAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<SessionDetailDto> UpdateGuestCountAsync(Guid sessionId, int guestCount, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<SessionDetailDto> AddOrderItemAsync(Guid sessionId, Guid menuItemId, int quantity, Guid userId, string? notes, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<SessionDetailDto> UpdateOrderItemQuantityAsync(Guid orderItemId, int quantity, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<SessionDetailDto> RemoveOrderItemAsync(Guid orderItemId, Guid userId, string? reason, bool adminOverride, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<SessionDetailDto> UpdateOrderItemNotesAsync(Guid orderItemId, string? notes, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<SessionDetailDto> ApplyDiscountAsync(Guid sessionId, decimal discountAmount, bool isPercentage, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<SessionDetailDto> RequestBillAsync(Guid sessionId, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<SessionDetailDto> RecordPaymentAsync(Guid sessionId, decimal amount, PaymentMethod method, Guid userId, decimal changeGiven, string? referenceNo, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task CancelSessionAsync(Guid sessionId, Guid userId, string reason, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<SessionDetailDto> MoveSessionAsync(Guid sessionId, Guid targetTableId, byte[] rowVersion, CancellationToken cancellationToken = default);
}
