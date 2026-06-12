namespace RestaurantOS.Domain.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string entityId, Guid? userId,
        string? oldValues = null, string? newValues = null, string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
