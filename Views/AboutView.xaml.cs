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
}
