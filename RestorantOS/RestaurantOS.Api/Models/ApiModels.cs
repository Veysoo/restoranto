using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Api.Models;

public record OpenSessionRequest(Guid TableId, int GuestCount = 2);

public record AddOrderRequest(Guid MenuItemId, int Quantity = 1, string? Notes = null);

public record PaymentRequest(decimal Amount, PaymentMethod Method, decimal ChangeGiven = 0, string? ReferenceNo = null);

public record RowVersionRequest(byte[] RowVersion);

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
