using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace RestaurantOS.WPF.Controls;

public class AnimatedNumber : TextBlock
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(AnimatedNumber),
            new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(nameof(Duration), typeof(int), typeof(AnimatedNumber), new PropertyMetadata(800));

    public static readonly DependencyProperty IsCurrencyProperty =
        DependencyProperty.Register(nameof(IsCurrency), typeof(bool), typeof(AnimatedNumber), new PropertyMetadata(true));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Duration
    {
        get => (int)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public bool IsCurrency
    {
        get => (bool)GetValue(IsCurrencyProperty);
        set => SetValue(IsCurrencyProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedNumber control)
            control.AnimateTo((double)e.NewValue);
    }

    private void AnimateTo(double target)
    {
        var animation = new DoubleAnimation(0, target, TimeSpan.FromMilliseconds(Duration))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        animation.CurrentTimeInvalidated += (_, _) => { };
        animation.Completed += (_, _) => UpdateText(target);

        var clock = animation.CreateClock();
        var start = DateTime.Now;
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        var from = 0.0;
        timer.Tick += (_, _) =>
        {
            var elapsed = (DateTime.Now - start).TotalMilliseconds;
            var progress = Math.Min(1, elapsed / Duration);
            var eased = 1 - Math.Pow(1 - progress, 3);
            UpdateText(from + (target - from) * eased);
            if (progress >= 1) timer.Stop();
        };
        timer.Start();
    }

    private void UpdateText(double val)
    {
        Text = IsCurrency
            ? val.ToString("₺#,##0.00", CultureInfo.GetCultureInfo("tr-TR"))
            : val.ToString("N0", CultureInfo.GetCultureInfo("tr-TR"));
    }
}
