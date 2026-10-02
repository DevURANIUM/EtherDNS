using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EtherDNS.Core;

/// <summary>
/// Copies text through the Win32 clipboard API with retries.
/// WPF's Clipboard.SetText ends with OleFlushClipboard, which throws CLIPBRD_E_CANT_OPEN whenever
/// clipboard history or a clipboard manager is reading the clipboard — even though the text was set.
/// </summary>
public static class ClipboardHelper
{
    const uint CF_UNICODETEXT = 13;
    const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll", SetLastError = true)] static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll", SetLastError = true)] static extern bool CloseClipboard();
    [DllImport("user32.dll", SetLastError = true)] static extern bool EmptyClipboard();
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetClipboardData(uint format, IntPtr data);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr GlobalLock(IntPtr mem);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GlobalUnlock(IntPtr mem);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr GlobalFree(IntPtr mem);

    public static bool TrySetText(string text)
    {
        // Must be a real window: with a NULL owner, SetClipboardData fails after EmptyClipboard.
        var owner = Application.Current?.MainWindow is { } w ? new WindowInteropHelper(w).Handle : IntPtr.Zero;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            if (TrySetOnce(owner, text)) return true;
            Thread.Sleep(25);
        }
        return false;
    }

    static bool TrySetOnce(IntPtr owner, string text)
    {
        if (!OpenClipboard(owner)) return false;
        try
        {
            if (!EmptyClipboard()) return false;

            int bytes = (text.Length + 1) * sizeof(char);
            IntPtr mem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
            if (mem == IntPtr.Zero) return false;

            IntPtr target = GlobalLock(mem);
            if (target == IntPtr.Zero)
            {
                GlobalFree(mem);
                return false;
            }
            Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
            Marshal.WriteInt16(target, text.Length * sizeof(char), 0);
            GlobalUnlock(mem);

            // On success the system owns the memory; only free it on failure.
            if (SetClipboardData(CF_UNICODETEXT, mem) == IntPtr.Zero)
            {
                GlobalFree(mem);
                return false;
            }
            return true;
        }
        finally
        {
            CloseClipboard();
        }
    }
}
