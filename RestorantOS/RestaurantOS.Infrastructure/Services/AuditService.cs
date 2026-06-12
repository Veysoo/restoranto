using RestaurantOS.Domain.Entities;
using RestaurantOS.Domain.Interfaces;
using RestaurantOS.Infrastructure.Data;

namespace RestaurantOS.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string action, string entityType, string entityId, Guid? userId,
        string? oldValues = null, string? newValues = null, string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            OldValues = oldValues,
            NewValues = newValues,
            IPAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
