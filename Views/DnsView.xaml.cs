using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EtherDNS.Core;

namespace EtherDNS.Views;

public partial class DnsView : UserControl
{
    readonly MainViewModel _vm = MainViewModel.Instance;

    public DnsView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsBenchmarking))
            {
                TestAllButton.IsEnabled = !_vm.IsBenchmarking;
                TestAllText.Text = _vm.IsBenchmarking ? "Testing…" : "Test All Speeds";
            }
        };
    }

    void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string cat }) _vm.Category = cat;
    }

    async void TestAll_Click(object sender, RoutedEventArgs e) => await _vm.BenchmarkAsync();
    async void Reset_Click(object sender, RoutedEventArgs e) => await _vm.ResetDnsAsync();
    async void RemoveAll_Click(object sender, RoutedEventArgs e) => await ((MainWindow)Window.GetWindow(this)).RemoveDnsEverywhereAsync();

    async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DnsItem item)
            await _vm.ApplyDnsAsync(item);
    }

    async void TestOne_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DnsItem item || item.IsTesting) return;
        item.IsTesting = true;
        try
        {
            item.Latency = await DnsProbe.MeasureAsync(item.Primary);
            item.IsTested = true;
        }
        finally
        {
            item.IsTesting = false;
        }
    }

    void CopyIp_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string ip })
        {
            if (ClipboardHelper.TrySetText(ip)) _vm.Notify($"Copied {ip}", ToastKind.Success);
            else _vm.Notify("Couldn't access the clipboard, try again.", ToastKind.Error);
        }
    }

    async void ApplyCustom_Click(object sender, RoutedEventArgs e) =>
        await _vm.ApplyDnsAsync("Custom DNS", CustomPrimary.Text, CustomSecondary.Text);

    async void TestCustom_Click(object sender, RoutedEventArgs e)
    {
        if (!NetworkService.IsValidIPv4(CustomPrimary.Text))
        {
            _vm.Notify("Enter a valid primary IPv4 address first.", ToastKind.Error);
            return;
        }

        CustomLatencyBadge.Visibility = Visibility.Visible;
        CustomLatency.Text = "Testing…";
        CustomLatency.Foreground = (Brush)FindResource("Accent1Brush");

        var ms = await _vm.TestServerAsync(CustomPrimary.Text);
        CustomLatency.Text = ms is int v ? $"{v} ms" : "Timeout";
        CustomLatency.Foreground = new SolidColorBrush(
            ms is null ? Color.FromRgb(0xF8, 0x71, 0x71) :
            ms < 60 ? Color.FromRgb(0x34, 0xD3, 0x99) :
            ms < 150 ? Color.FromRgb(0xFB, 0xBF, 0x24) : Color.FromRgb(0xFB, 0x92, 0x3C));
    }
}
