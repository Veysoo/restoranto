using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOS.Application.DTOs.Auth;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.WPF.Services;

namespace RestaurantOS.WPF.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly CurrentUserService _currentUser;
    private readonly LocalSettingsService _localSettings;
    public Action? OnLoginSuccess { get; set; }

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _version = "v1.0.0";

    public LoginViewModel(IAuthService authService, CurrentUserService currentUser,
        LocalSettingsService localSettings)
    {
        _authService = authService;
        _currentUser = currentUser;
        _localSettings = localSettings;
        Username = localSettings.GetLastUsername() ?? "admin";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Kullanıcı adı ve şifre gerekli.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _authService.LoginAsync(new LoginRequest { Username = Username, Password = Password });
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Giriş başarısız.";
                return;
            }

            _currentUser.SetUser(result.UserId, result.FullName, result.Username, result.Role);
            _localSettings.SaveLastUsername(Username);
            OnLoginSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Bağlantı hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
