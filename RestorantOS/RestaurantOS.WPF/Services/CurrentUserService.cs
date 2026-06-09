using RestaurantOS.Domain.Enums;

namespace RestaurantOS.WPF.Services;

public class CurrentUserService
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public void SetUser(Guid userId, string fullName, string username, UserRole role)
    {
        UserId = userId;
        FullName = fullName;
        Username = username;
        Role = role;
        IsAuthenticated = true;
    }

    public void Clear()
    {
        UserId = Guid.Empty;
        FullName = string.Empty;
        Username = string.Empty;
        IsAuthenticated = false;
    }

    public bool IsAdmin => Role == UserRole.Admin;
}
