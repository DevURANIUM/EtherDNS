using System.Windows;
using System.Windows.Threading;
using EtherDNS.Core;

namespace EtherDNS;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnUnhandled;
        base.OnStartup(e);
    }

    static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MainViewModel.Instance.Notify("Unexpected error: " + e.Exception.Message, ToastKind.Error);
        e.Handled = true;
    }
}
