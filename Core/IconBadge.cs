using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EtherDNS.Core;

/// <summary>
/// Glossy app-icon style badge: two-tone gradient body, top highlight, light rim and a soft
/// colored glow underneath. Shows either an icon glyph or a letter.
///   &lt;core:IconBadge Glyph="&amp;#xE74D;" Tint="blue" Size="38"/&gt;
///   &lt;core:IconBadge Letter="{Binding Initial}" Brush="{Binding Accent}" Size="38"/&gt;
/// </summary>
public sealed class IconBadge : Control
{
    static readonly Dictionary<string, (string Top, string Bottom)> Tints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["brand"] = ("#60A5FA", "#4F46E5"),
        ["blue"] = ("#5AB0FF", "#2563EB"),
        ["indigo"] = ("#A5B4FC", "#4F46E5"),
        ["purple"] = ("#D8A4FE", "#7C3AED"),
        ["pink"] = ("#FF8FB8", "#E11D48"),
        ["red"] = ("#FF9A8F", "#DC2626"),
        ["orange"] = ("#FFCB7A", "#EA580C"),
        ["yellow"] = ("#FDE68A", "#D97706"),
        ["green"] = ("#7EEBA8", "#15803D"),
        ["teal"] = ("#7DEBFB", "#0E7490"),
        ["gray"] = ("#B4BAC6", "#4B5563"),
    };

    static readonly Dictionary<string, Brush> Cache = new(StringComparer.OrdinalIgnoreCase);

    static IconBadge()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(IconBadge), new FrameworkPropertyMetadata(typeof(IconBadge)));
        FocusableProperty.OverrideMetadata(typeof(IconBadge), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(IconBadge), new FrameworkPropertyMetadata(false));
    }

    public IconBadge() => Update();

    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(IconBadge), new PropertyMetadata(""));

    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

    public static readonly DependencyProperty LetterProperty = DependencyProperty.Register(
        nameof(Letter), typeof(string), typeof(IconBadge), new PropertyMetadata(""));

    public string Letter { get => (string)GetValue(LetterProperty); set => SetValue(LetterProperty, value); }

    public static readonly DependencyProperty TintProperty = DependencyProperty.Register(
        nameof(Tint), typeof(string), typeof(IconBadge), new PropertyMetadata("brand", (d, _) => ((IconBadge)d).Update()));

    public string Tint { get => (string)GetValue(TintProperty); set => SetValue(TintProperty, value); }

    /// <summary>Explicit fill; overrides <see cref="Tint"/>.</summary>
    public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
        nameof(Brush), typeof(Brush), typeof(IconBadge), new PropertyMetadata(null, (d, _) => ((IconBadge)d).Update()));

    public Brush? Brush { get => (Brush?)GetValue(BrushProperty); set => SetValue(BrushProperty, value); }

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(double), typeof(IconBadge), new PropertyMetadata(38.0, (d, _) => ((IconBadge)d).Update()));

    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    // ---- Computed values consumed by the template ----

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(IconBadge));

    public Brush Fill { get => (Brush)GetValue(FillProperty); private set => SetValue(FillProperty, value); }

    public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(
        nameof(Radius), typeof(CornerRadius), typeof(IconBadge));

    public CornerRadius Radius { get => (CornerRadius)GetValue(RadiusProperty); private set => SetValue(RadiusProperty, value); }

    public static readonly DependencyProperty GlyphSizeProperty = DependencyProperty.Register(
        nameof(GlyphSize), typeof(double), typeof(IconBadge));

    public double GlyphSize { get => (double)GetValue(GlyphSizeProperty); private set => SetValue(GlyphSizeProperty, value); }

    void Update()
    {
        double size = Size;
        Width = size;
        Height = size;
        Radius = new CornerRadius(Math.Round(size * 0.27));
        GlyphSize = Math.Round(size * 0.46);
        Fill = Brush ?? TintBrush(Tint);
    }

    static Brush TintBrush(string tint)
    {
        if (Cache.TryGetValue(tint, out var cached)) return cached;
        var (top, bottom) = Tints.TryGetValue(tint, out var t) ? t : Tints["brand"];
        var brush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString(top),
            (Color)ColorConverter.ConvertFromString(bottom),
            new Point(0.2, 0), new Point(0.8, 1));
        brush.Freeze();
        Cache[tint] = brush;
        return brush;
    }
}
