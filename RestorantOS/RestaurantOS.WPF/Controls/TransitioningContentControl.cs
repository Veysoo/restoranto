using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RestaurantOS.WPF.Controls;

public class TransitioningContentControl : ContentControl
{
    private ContentPresenter? _presenter;
    private bool _isFirstLoad = true;

    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register(nameof(Direction), typeof(int), typeof(TransitioningContentControl), new PropertyMetadata(1));

    public int Direction
    {
        get => (int)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _presenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (_presenter == null || newContent == null) return;

        if (_isFirstLoad)
        {
            _isFirstLoad = false;
            ApplyEnterAnimation(_presenter, skipSlide: true);
            return;
        }

        if (oldContent != null && _presenter.RenderTransform is not TransformGroup)
        {
            var group = new TransformGroup();
            group.Children.Add(new TranslateTransform());
            group.Children.Add(new ScaleTransform(1, 1));
            _presenter.RenderTransform = group;
            _presenter.Opacity = 1;
        }

        var exitStoryboard = CreateExitStoryboard(_presenter);
        exitStoryboard.Completed += (_, _) =>
        {
            ApplyEnterAnimation(_presenter);
        };
        exitStoryboard.Begin();
    }

    private void ApplyEnterAnimation(ContentPresenter presenter, bool skipSlide = false)
    {
        var group = new TransformGroup();
        var translate = new TranslateTransform(skipSlide ? 0 : 40 * Direction, 0);
        group.Children.Add(translate);
        presenter.RenderTransform = group;
        presenter.Opacity = skipSlide ? 1 : 0;

        var storyboard = new Storyboard();
        if (!skipSlide)
        {
            var slide = new DoubleAnimation(40 * Direction, 0, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(slide, translate);
            Storyboard.SetTargetProperty(slide, new PropertyPath(TranslateTransform.XProperty));
            storyboard.Children.Add(slide);

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(fadeIn, presenter);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);
        }
        storyboard.Begin();
    }

    private Storyboard CreateExitStoryboard(ContentPresenter presenter)
    {
        var group = presenter.RenderTransform as TransformGroup ?? new TransformGroup();
        if (group.Children.Count == 0) group.Children.Add(new TranslateTransform());
        presenter.RenderTransform = group;
        var translate = (TranslateTransform)group.Children[0];

        var storyboard = new Storyboard();
        var slide = new DoubleAnimation(0, -30 * Direction, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(slide, translate);
        Storyboard.SetTargetProperty(slide, new PropertyPath(TranslateTransform.XProperty));
        storyboard.Children.Add(slide);

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(fadeOut, presenter);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(fadeOut);

        return storyboard;
    }
}
