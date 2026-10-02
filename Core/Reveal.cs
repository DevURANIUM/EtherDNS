using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace EtherDNS.Core;

/// <summary>
/// Entrance animations, replayed every time an element is (re)loaded — i.e. on each page visit.
///   ui:Reveal.Delay="0:0:0.1"   fade + slide-up a single element after a delay
///   ui:Reveal.Stagger="True"    same, applied one after another to a Panel's children or an ItemsControl's items
/// </summary>
public static class Reveal
{
    static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(460);
    static readonly TimeSpan Step = TimeSpan.FromMilliseconds(45);
    const int MaxStaggered = 24;

    public static readonly DependencyProperty DelayProperty = DependencyProperty.RegisterAttached(
        "Delay", typeof(TimeSpan?), typeof(Reveal), new PropertyMetadata(null, OnDelayChanged));

    public static TimeSpan? GetDelay(DependencyObject d) => (TimeSpan?)d.GetValue(DelayProperty);
    public static void SetDelay(DependencyObject d, TimeSpan? value) => d.SetValue(DelayProperty, value);

    public static readonly DependencyProperty StaggerProperty = DependencyProperty.RegisterAttached(
        "Stagger", typeof(bool), typeof(Reveal), new PropertyMetadata(false, OnStaggerChanged));

    public static bool GetStagger(DependencyObject d) => (bool)d.GetValue(StaggerProperty);
    public static void SetStagger(DependencyObject d, bool value) => d.SetValue(StaggerProperty, value);

    static void OnDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe) return;
        fe.Loaded -= OnDelayLoaded;
        if (e.NewValue != null) fe.Loaded += OnDelayLoaded;
    }

    static void OnDelayLoaded(object sender, RoutedEventArgs e)
    {
        var fe = (FrameworkElement)sender;
        Play(fe, GetDelay(fe) ?? TimeSpan.Zero);
    }

    static void OnStaggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe) return;
        fe.Loaded -= OnStaggerLoaded;
        if ((bool)e.NewValue) fe.Loaded += OnStaggerLoaded;
    }

    static void OnStaggerLoaded(object sender, RoutedEventArgs e)
    {
        var fe = (FrameworkElement)sender;
        // Item containers are generated after Loaded; wait for layout.
        fe.Dispatcher.BeginInvoke(() =>
        {
            var children = Children(fe).Take(MaxStaggered).ToList();
            for (int i = 0; i < children.Count; i++)
                Play(children[i], TimeSpan.FromTicks(Step.Ticks * i));
        }, DispatcherPriority.Loaded);
    }

    static IEnumerable<FrameworkElement> Children(FrameworkElement fe)
    {
        if (fe is ItemsControl items)
        {
            for (int i = 0; i < items.Items.Count; i++)
                if (items.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement c)
                    yield return c;
        }
        else if (fe is Panel panel)
        {
            foreach (var c in panel.Children.OfType<FrameworkElement>())
                if (c.Visibility == Visibility.Visible) yield return c;
        }
    }

    static void Play(FrameworkElement fe, TimeSpan delay)
    {
        var translate = new TranslateTransform(0, 16);
        fe.RenderTransform = translate;
        fe.Opacity = 0;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var fade = new DoubleAnimation(0, 1, Duration) { BeginTime = delay, EasingFunction = ease };
        var slide = new DoubleAnimation(16, 0, Duration) { BeginTime = delay, EasingFunction = ease };

        // Release the animated value at the end so later Opacity setters keep working.
        fade.Completed += (_, _) =>
        {
            fe.BeginAnimation(UIElement.OpacityProperty, null);
            fe.Opacity = 1;
        };

        fe.BeginAnimation(UIElement.OpacityProperty, fade);
        translate.BeginAnimation(TranslateTransform.YProperty, slide);
    }
}
