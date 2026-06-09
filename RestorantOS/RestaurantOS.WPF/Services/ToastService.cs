using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RestaurantOS.WPF.Services;

public class ToastMessage
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public Guid Id { get; set; } = Guid.NewGuid();
}

public class ToastService
{
    private readonly ObservableCollection<ToastMessage> _messages = new();
    private Panel? _container;
    private const int MaxToasts = 3;

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public void Initialize(Panel container) => _container = container;

    public void Show(string title, string message, string type = "info")
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            while (_messages.Count >= MaxToasts)
                Dismiss(_messages[0]);

            var toast = new ToastMessage { Title = title, Message = message, Type = type };
            _messages.Add(toast);
            RenderToast(toast);

            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            timer.Tick += (_, _) => { timer.Stop(); Dismiss(toast); };
            timer.Start();
        });
    }

    public void Success(string message) => Show("Başarılı", message, "success");
    public void Error(string message) => Show("Hata", message, "error");
    public void Warning(string message) => Show("Uyarı", message, "warning");
    public void Info(string message) => Show("Bilgi", message, "info");

    private void RenderToast(ToastMessage toast)
    {
        if (_container == null) return;

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(33, 37, 58)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(45, 49, 72)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Width = 320,
            Tag = toast.Id,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Opacity = 0.3 }
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = toast.Title, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, FontSize = 13 });
        stack.Children.Add(new TextBlock { Text = toast.Message, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), FontSize = 12, Margin = new Thickness(0, 4, 0, 0) });
        border.Child = stack;

        var transform = new TranslateTransform(0, -80);
        border.RenderTransform = transform;
        border.Opacity = 0;
        _container.Children.Add(border);

        var slideIn = new DoubleAnimation(-80, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
        transform.BeginAnimation(TranslateTransform.YProperty, slideIn);
        border.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    }

    private void Dismiss(ToastMessage toast)
    {
        _messages.Remove(toast);
        if (_container == null) return;

        var element = _container.Children.OfType<Border>().FirstOrDefault(b => (Guid)b.Tag! == toast.Id);
        if (element == null) return;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        fadeOut.Completed += (_, _) => _container.Children.Remove(element);
        element.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }
}
