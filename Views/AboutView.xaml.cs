using System.Windows;
using System.Windows.Controls;
using EtherDNS.Core;

namespace EtherDNS.Views;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
        DataContext = MainViewModel.Instance;
    }

    void Link_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url }) AdminHelper.OpenUrl(url);
    }

    async void CheckUpdate_Click(object sender, RoutedEventArgs e) => await MainViewModel.Instance.CheckForUpdatesAsync(silent: false);
    async void InstallUpdate_Click(object sender, RoutedEventArgs e) => await MainViewModel.Instance.InstallUpdateAsync();
    void CancelUpdate_Click(object sender, RoutedEventArgs e) => MainViewModel.Instance.CancelUpdateDownload();

    void ReleaseNotes_Click(object sender, RoutedEventArgs e) =>
        AdminHelper.OpenUrl(MainViewModel.Instance.AvailableUpdate?.PageUrl ?? UpdateService.ReleasesPage);
}
