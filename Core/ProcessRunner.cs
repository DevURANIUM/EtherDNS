using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace EtherDNS.Core;

public static class ProcessRunner
{
    [DllImport("kernel32.dll")]
    static extern int GetOEMCP();

    // netsh / ipconfig write in the console OEM code page, not UTF-8.
    static readonly Encoding ConsoleEncoding = ResolveEncoding();

    static Encoding ResolveEncoding()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(GetOEMCP());
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static async Task<(int Code, string Out, string Err)> RunAsync(string file, string args, TimeSpan? timeout = null)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = ConsoleEncoding,
            StandardErrorEncoding = ConsoleEncoding,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start {file}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeout ?? DefaultTimeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"{file} did not respond in time and was stopped.");
        }

        return (process.ExitCode, await stdout, await stderr);
    }

    public static async Task<string> RunOrThrowAsync(string file, string args, TimeSpan? timeout = null)
    {
        var (code, stdout, stderr) = await RunAsync(file, args, timeout);
        if (code != 0)
        {
            string msg = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(msg) ? $"{file} failed (exit code {code})." : msg.Trim());
        }
        return stdout;
    }
}
