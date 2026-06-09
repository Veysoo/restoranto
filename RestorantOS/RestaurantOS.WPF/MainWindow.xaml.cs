using System.Windows;
using System.Windows.Input;
using RestaurantOS.WPF.Services;
using RestaurantOS.WPF.ViewModels;

namespace RestaurantOS.WPF;

public partial class MainWindow : Window
{
    private readonly ToastService _toast;

    public MainWindow(MainViewModel viewModel, ToastService toast)
    {
        InitializeComponent();
        DataContext = viewModel;
        _toast = toast;
        Loaded += (_, _) => _toast.Initialize(ToastPanel);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

}
