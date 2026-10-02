using System.Windows;
using System.Windows.Controls;
using EtherDNS.Core;

namespace EtherDNS.Views;

public partial class DashboardView : UserControl
{
    readonly MainViewModel _vm = MainViewModel.Instance;

    public DashboardView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.HasFastest)) UpdateFastestVisibility();
        };
        UpdateFastestVisibility();
    }

    void UpdateFastestVisibility()
    {
        EmptyFastest.Visibility = _vm.HasFastest ? Visibility.Collapsed : Visibility.Visible;
        FastestList.Visibility = _vm.HasFastest ? Visibility.Visible : Visibility.Collapsed;
    }

    void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _vm.RefreshAdapters();
        _vm.Notify("Network status refreshed", ToastKind.Info);
    }

    void Browse_Click(object sender, RoutedEventArgs e) => ((MainWindow)Window.GetWindow(this)).NavigateTo("dns");
    async void Reset_Click(object sender, RoutedEventArgs e) => await _vm.ResetDnsAsync();
    async void Flush_Click(object sender, RoutedEventArgs e) => await _vm.FlushDnsAsync();
    async void Renew_Click(object sender, RoutedEventArgs e) => await _vm.RenewIpAsync();
    async void FullReset_Click(object sender, RoutedEventArgs e) => await _vm.FullResetAsync();
    async void Benchmark_Click(object sender, RoutedEventArgs e) => await _vm.BenchmarkAsync();

    async void ApplyFastest_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DnsItem item)
            await _vm.ApplyDnsAsync(item);
    }
}
