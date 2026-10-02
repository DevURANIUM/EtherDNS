using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using EtherDNS.Core;
using EtherDNS.Views;

namespace EtherDNS;

public partial class MainWindow : Window
{
    readonly MainViewModel _vm = MainViewModel.Instance;
    readonly Dictionary<string, FrameworkElement> _pages = new();
    readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(3.6) };
    readonly DispatcherTimer _networkDebounce = new() { Interval = TimeSpan.FromMilliseconds(900) };

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        RestartAdminButton.Visibility = _vm.IsAdmin ? Visibility.Collapsed : Visibility.Visible;

        _vm.Toast += ShowToast;
        _toastTimer.Tick += (_, _) => HideToast();

        // Keep the dashboard live when Windows reports address/DNS changes.
        _networkDebounce.Tick += (_, _) =>
        {
            _networkDebounce.Stop();
            if (!_vm.IsBusy) _vm.RefreshAdapters();
            // Public IP changes when a VPN connects/disconnects.
            if (_vm.HasPublicIp) _ = _vm.LoadIpInfoAsync();
        };
        NetworkChange.NetworkAddressChanged += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            _networkDebounce.Stop();
            _networkDebounce.Start();
        });

        SourceInitialized += (_, _) => ApplyNativeChrome();
        StateChanged += (_, _) => OnWindowStateChanged();
        Loaded += (_, _) =>
        {
            _vm.RefreshAdapters();
            NavDashboard.IsChecked = true;
            _vm.SetLiveMonitor(true);
        };
    }

    // ---------- Navigation ----------

    public void NavigateTo(string page)
    {
        var target = page switch
        {
            "dns" => NavDns,
            "ip" => NavIp,
            "wifi" => NavWifi,
            "about" => NavAbout,
            _ => NavDashboard,
        };
        target.IsChecked = true;
    }

    void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (PageHost == null) return;

        (string key, string title) = sender switch
        {
            _ when sender == NavDns => ("dns", "DNS Servers"),
            _ when sender == NavIp => ("ip", "IP Info"),
            _ when sender == NavWifi => ("wifi", "Wi-Fi"),
            _ when sender == NavAbout => ("about", "About"),
            _ => ("dashboard", "Overview"),
        };

        if (!_pages.TryGetValue(key, out var page))
        {
            page = key switch
            {
                "dns" => new DnsView(),
                "ip" => new IpInfoView(),
                "wifi" => new WifiView(),
                "about" => new AboutView(),
                _ => new DashboardView(),
            };
            _pages[key] = page;
        }

        Breadcrumb.Text = title;
        PageHost.Content = page;
        AnimateIn(page);
        MoveNavIndicator((FrameworkElement)sender);
    }

    void MoveNavIndicator(FrameworkElement target)
    {
        // Before the first layout pass positions are unknown; retry once laid out.
        if (!target.IsLoaded || target.ActualHeight == 0)
        {
            Dispatcher.BeginInvoke(() => MoveNavIndicator(target), DispatcherPriority.Loaded);
            return;
        }

        double y = target.TransformToAncestor(NavPanel).Transform(new Point(0, 0)).Y;
        var translate = (TranslateTransform)NavIndicator.RenderTransform;

        if (NavIndicator.Opacity == 0)
        {
            translate.Y = y;
            NavIndicator.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
            return;
        }

        translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(y, TimeSpan.FromMilliseconds(420))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 },
        });
    }

    static void AnimateIn(FrameworkElement page)
    {
        var translate = new TranslateTransform(0, 18);
        page.RenderTransform = translate;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(380);

        page.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
        translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(18, 0, duration) { EasingFunction = ease });
    }

    // ---------- Confirm dialog ----------

    TaskCompletionSource<bool>? _confirm;

    public Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        _confirm?.TrySetResult(false);
        _confirm = new TaskCompletionSource<bool>();

        ConfirmTitle.Text = title;
        ConfirmMessage.Text = message;
        ConfirmYes.Content = confirmText;
        ConfirmOverlay.Visibility = Visibility.Visible;
        ConfirmOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
        return _confirm.Task;
    }

    void CloseConfirm(bool result)
    {
        ConfirmOverlay.Visibility = Visibility.Collapsed;
        _confirm?.TrySetResult(result);
        _confirm = null;
    }

    public async Task RemoveDnsEverywhereAsync()
    {
        var adapters = NetworkService.GetAdaptersWithStaticDns();
        if (adapters.Count == 0)
        {
            _vm.Notify("No adapter has custom DNS — nothing to remove.", ToastKind.Info);
            return;
        }

        bool ok = await ConfirmAsync(
            "Remove DNS from all adapters?",
            $"Manually set DNS will be removed from {adapters.Count} adapter{(adapters.Count == 1 ? "" : "s")}:\n" +
            $"{string.Join(", ", adapters)}\n\nThey will go back to automatic DNS from your router (DHCP).",
            "Remove DNS");

        if (ok) await _vm.RemoveDnsEverywhereAsync();
    }

    void ConfirmYes_Click(object sender, RoutedEventArgs e) => CloseConfirm(true);
    void ConfirmNo_Click(object sender, RoutedEventArgs e) => CloseConfirm(false);

    // ---------- Toast ----------

    void ShowToast(string message, ToastKind kind)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => ShowToast(message, kind));
            return;
        }

        (string icon, string color) = kind switch
        {
            ToastKind.Error => ("\uE783", "#FF453A"),
            ToastKind.Info => ("\uE946", "#0A84FF"),
            _ => ("\uE73E", "#30D158"),
        };
        var c = (Color)ColorConverter.ConvertFromString(color);

        ToastText.Text = message;
        ToastIcon.Text = icon;
        ToastIcon.Foreground = Brushes.White;
        ToastIconBg.Background = new SolidColorBrush(c);
        ToastBar.Background = new SolidColorBrush(Color.FromArgb(0x99, c.R, c.G, c.B));
        ((ScaleTransform)ToastBar.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(1, 0, _toastTimer.Interval));

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        Toast.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
        ((TranslateTransform)Toast.RenderTransform).BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(0, TimeSpan.FromMilliseconds(320)) { EasingFunction = ease });

        _toastTimer.Stop();
        _toastTimer.Start();
    }

    void HideToast()
    {
        _toastTimer.Stop();
        Toast.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
        ((TranslateTransform)Toast.RenderTransform).BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(40, TimeSpan.FromMilliseconds(250)));
    }

    // ---------- Window chrome ----------

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    const int DWMWCP_ROUND = 2;
    const int DWMSBT_TRANSIENTWINDOW = 3; // acrylic

    /// <summary>
    /// Native Windows 11 chrome: rounded corners, the soft system shadow and an acrylic backdrop
    /// that shows through the translucent sidebar (like macOS vibrancy).
    /// </summary>
    void ApplyNativeChrome()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (HwndSource.FromHwnd(hwnd) is { CompositionTarget: not null } source)
            source.CompositionTarget.BackgroundColor = Colors.Transparent;

        int on = 1, round = DWMWCP_ROUND, backdrop = DWMSBT_TRANSIENTWINDOW;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int));
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));

        // System backdrops exist from Windows 11 22H2 (build 22621); older systems get opaque surfaces.
        bool backdropOk = Environment.OSVersion.Version.Build >= 22621 &&
                          DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int)) == 0;
        if (!backdropOk)
        {
            Resources["SidebarBrush"] = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x26));
            Resources["BgBrush"] = new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x1E));
        }
    }

    void OnWindowStateChanged()
    {
        // No point probing DNS while nobody can see the chart.
        _vm.SetLiveMonitor(WindowState != WindowState.Minimized);

        // A maximized borderless window overhangs the screen by its resize frame; pad it back in.
        var frame = SystemParameters.WindowResizeBorderThickness;
        Root.Margin = WindowState == WindowState.Maximized
            ? new Thickness(frame.Left + 4, frame.Top + 4, frame.Right + 4, frame.Bottom + 4)
            : new Thickness(0);
    }

    void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try { DragMove(); } catch (InvalidOperationException) { }
        }
    }

    void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            return;
        }
        // Borderless windows would otherwise cover the taskbar when maximized.
        var area = SystemParameters.WorkArea;
        MaxHeight = area.Height + 16;
        MaxWidth = area.Width + 16;
        WindowState = WindowState.Maximized;
    }

    void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    void Close_Click(object sender, RoutedEventArgs e) => Close();

    void RestartAdmin_Click(object sender, RoutedEventArgs e)
    {
        if (AdminHelper.RestartElevated()) Application.Current.Shutdown();
        else _vm.Notify("Elevation was cancelled.", ToastKind.Info);
    }
}
