using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using EtherDNS.Core;

namespace EtherDNS.Views;

public partial class IpInfoView : UserControl
{
    readonly MainViewModel _vm = MainViewModel.Instance;

    public IpInfoView()
    {
        InitializeComponent();
        DataContext = _vm;

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainViewModel.IsLoadingIp) or nameof(MainViewModel.HasPublicIp) or nameof(MainViewModel.HasIpError))
                UpdateStates();
        };
        UpdateStates();

        Loaded += async (_, _) =>
        {
            ScrollToTopSoon();
            if (!_vm.HasPublicIp && !_vm.IsLoadingIp) await _vm.LoadIpInfoAsync();
        };
    }

    // Content appears after the async lookup; reset the offset once layout has settled.
    void ScrollToTopSoon() =>
        Dispatcher.BeginInvoke(Scroller.ScrollToTop, System.Windows.Threading.DispatcherPriority.Loaded);

    void UpdateStates()
    {
        if (_vm.HasPublicIp) ScrollToTopSoon();
        FirstLoad.Visibility = _vm.IsLoadingIp && !_vm.HasPublicIp ? Visibility.Visible : Visibility.Collapsed;
        ErrorCard.Visibility = !_vm.IsLoadingIp && _vm.HasIpError && !_vm.HasPublicIp ? Visibility.Visible : Visibility.Collapsed;
    }

    async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadIpInfoAsync();
        if (_vm.HasIpError && _vm.HasPublicIp) _vm.Notify(_vm.IpError!, ToastKind.Error);
    }

    void CopyIp_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.PublicIp is { } ip) Copy(ip.Ip, $"Copied {ip.Ip}");
    }

    void CopyAll_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.PublicIp is not { } ip) return;
        var text =
            $"Public IP:   {ip.Ip}\n" +
            $"Location:    {ip.Location}\n" +
            $"ISP:         {ip.IspText}\n" +
            $"Org:         {ip.OrgText}\n" +
            $"ASN:         {ip.AsnText}\n" +
            $"Timezone:    {ip.TimezoneText}\n" +
            $"Coordinates: {ip.CoordinatesText}";
        Copy(text, "Connection details copied");
    }

    void Map_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.PublicIp is { HasCoordinates: true } ip)
            AdminHelper.OpenUrl(string.Create(CultureInfo.InvariantCulture,
                $"https://www.google.com/maps/search/?api=1&query={ip.Lat},{ip.Lon}"));
    }

    void Copy(string text, string message)
    {
        if (ClipboardHelper.TrySetText(text)) _vm.Notify(message, ToastKind.Success);
        else _vm.Notify("Couldn't access the clipboard, try again.", ToastKind.Error);
    }
}
