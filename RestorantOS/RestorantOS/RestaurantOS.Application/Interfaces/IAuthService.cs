using RestaurantOS.Application.DTOs.Auth;

namespace RestaurantOS.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<bool> ValidatePasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);
}
