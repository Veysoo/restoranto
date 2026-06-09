using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestaurantOS.Infrastructure;
using RestaurantOS.Infrastructure.Data;
using RestaurantOS.Infrastructure.Data.Seed;
using RestaurantOS.WPF.Services;
using RestaurantOS.WPF.ViewModels;

namespace RestaurantOS.WPF;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnLastWindowClose;

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"Beklenmeyen hata:\n{args.Exception.Message}", "RestaurantOS",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory);
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var conn = ctx.Configuration.GetConnectionString("DefaultConnection")!;
                services.AddInfrastructure(conn);

                services.AddSingleton<NavigationService>();
                services.AddSingleton<CurrentUserService>();
                services.AddSingleton<ToastService>();
                services.AddSingleton<LocalSettingsService>();

                services.AddTransient<LoginViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<FloorViewModel>();
                services.AddTransient<OrdersViewModel>();
                services.AddTransient<MenuViewModel>();
                services.AddTransient<ReportsViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<SessionDetailViewModel>(sp =>
                {
                    throw new InvalidOperationException("Use factory for SessionDetailViewModel");
                });

                services.AddTransient<LoginWindow>();
                services.AddTransient<MainWindow>();

                services.AddSingleton<Func<Guid, SessionDetailViewModel>>(sp => sessionId =>
                {
                    return new SessionDetailViewModel(
                        sessionId,
                        sp.GetRequiredService<Application.Interfaces.ISessionService>(),
                        sp.GetRequiredService<Application.Interfaces.IMenuService>(),
                        sp.GetRequiredService<CurrentUserService>(),
                        sp.GetRequiredService<ToastService>());
                });

                services.AddTransient<FloorViewModel>(sp => new FloorViewModel(
                    sp.GetRequiredService<Application.Interfaces.IFloorService>(),
                    sp.GetRequiredService<Application.Interfaces.ISessionService>(),
                    sp.GetRequiredService<CurrentUserService>(),
                    sp.GetRequiredService<ToastService>(),
                    sp.GetRequiredService<Func<Guid, SessionDetailViewModel>>()));
            })
            .Build();

        await _host.StartAsync();

        try
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DatabaseSeeder.SeedAsync(db);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Veritabanı başlatılamadı:\n{ex.Message}\n\n" +
                "Çözüm:\n" +
                "1. Docker Desktop'ın çalıştığından emin olun\n" +
                "2. Terminalde: docker-compose down -v\n" +
                "3. Sonra: docker-compose up -d\n" +
                "4. 60 saniye bekleyip Startup.bat'ı tekrar çalıştırın\n\n" +
                "Giriş ekranı açılacak; veritabanı hazır olunca tekrar deneyin.",
                "RestaurantOS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        try
        {
            var login = _host.Services.GetRequiredService<LoginWindow>();
            login.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Uygulama başlatılamadı:\n{ex.Message}", "RestaurantOS",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
