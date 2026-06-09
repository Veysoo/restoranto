using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RestaurantOS.WPF.Controls;

public class TiltCard : Border
{
    private SkewTransform? _skew;

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        if (_skew == null)
        {
            _skew = new SkewTransform();
            RenderTransform = _skew;
            RenderTransformOrigin = new Point(0.5, 0.5);
        }

        var pos = e.GetPosition(this);
        _skew.AngleX = -(pos.Y / ActualHeight - 0.5) * 6;
        _skew.AngleY = (pos.X / ActualWidth - 0.5) * 6;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_skew == null) return;

        var animX = new DoubleAnimation(_skew.AngleX, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var animY = new DoubleAnimation(_skew.AngleY, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        _skew.BeginAnimation(SkewTransform.AngleXProperty, animX);
        _skew.BeginAnimation(SkewTransform.AngleYProperty, animY);
    }
}
