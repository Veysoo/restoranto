using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RestaurantOS.WPF.ViewModels;

namespace RestaurantOS.WPF;

public partial class LoginWindow : Window
{
    private readonly IServiceProvider _services;

    public LoginWindow(IServiceProvider services, LoginViewModel viewModel)
    {
        InitializeComponent();
        _services = services;
        viewModel.OnLoginSuccess = OnLoginSuccess;
        DataContext = viewModel;
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = PasswordBox.Password;
    }

    public void OnLoginSuccess()
    {
        var main = _services.GetRequiredService<MainWindow>();
        main.Show();
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
