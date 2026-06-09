namespace RestaurantOS.WPF.Services;

public class NavigationService
{
    public event Action<Type>? Navigated;

    private readonly IServiceProvider _provider;
    private object? _currentViewModel;

    public NavigationService(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object? CurrentViewModel => _currentViewModel;

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        _currentViewModel = _provider.GetService(typeof(TViewModel));
        Navigated?.Invoke(typeof(TViewModel));
    }
}
