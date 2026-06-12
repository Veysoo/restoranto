using Microsoft.EntityFrameworkCore;
using RestaurantOS.Application.DTOs.Auth;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Infrastructure.Data;

namespace RestaurantOS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new LoginResult { Success = false, ErrorMessage = "Kullanıcı adı veya şifre hatalı." };
        }

        return new LoginResult
        {
            Success = true,
            UserId = user.UserId,
            FullName = user.FullName,
            Username = user.Username,
            Role = user.Role
        };
    }

    public async Task<bool> ValidatePasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        return user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }
}
