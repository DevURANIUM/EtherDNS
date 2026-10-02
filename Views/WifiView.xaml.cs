using System.Windows;
using System.Windows.Controls;
using EtherDNS.Core;

namespace EtherDNS.Views;

public partial class WifiView : UserControl
{
    readonly MainViewModel _vm = MainViewModel.Instance;
    bool _loadedOnce;

    public WifiView()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += async (_, _) =>
        {
            if (_loadedOnce) return;
            _loadedOnce = true;
            await _vm.LoadWifiAsync();
        };
    }

    async void Refresh_Click(object sender, RoutedEventArgs e) => await _vm.LoadWifiAsync();

    void Reveal_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is WifiProfile p) p.IsRevealed = !p.IsRevealed;
    }

    void Copy_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not WifiProfile { HasPassword: true } p) return;
        if (ClipboardHelper.TrySetText(p.Password!)) _vm.Notify($"Password for \"{p.Name}\" copied", ToastKind.Success);
        else _vm.Notify("Couldn't access the clipboard, try again.", ToastKind.Error);
    }
}
