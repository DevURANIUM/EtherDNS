using System.Text.RegularExpressions;

namespace EtherDNS.Core;

public sealed class WifiProfile : ObservableObject
{
    public WifiProfile(string name, string? password, string authentication)
    {
        Name = name;
        Password = password;
        Authentication = authentication;
    }

    public string Name { get; }
    public string? Password { get; }
    public string Authentication { get; }
    public bool HasPassword => !string.IsNullOrEmpty(Password);

    bool _isRevealed;
    public bool IsRevealed
    {
        get => _isRevealed;
        set { if (Set(ref _isRevealed, value)) OnPropertyChanged(nameof(DisplayPassword)); }
    }

    public string DisplayPassword =>
        !HasPassword ? "Open network / no key" :
        IsRevealed ? Password! : "••••••••••";
}

public static class WifiService
{
    // Profile lines look like "    All User Profile     : Name" (label is localized, layout is not).
    static readonly Regex ProfileLine = new(@"^\s{4}[^:\r\n]+?\s+:\s(.+)$", RegexOptions.Multiline);
    static readonly Regex KeyLine = new(@"Key Content\s*:\s*(.*)");
    static readonly Regex AuthLine = new(@"Authentication\s*:\s*(.*)");

    public static async Task<List<WifiProfile>> GetProfilesAsync()
    {
        var (code, output, _) = await ProcessRunner.RunAsync("netsh", "wlan show profiles");
        if (code != 0 || string.IsNullOrWhiteSpace(output))
            return new List<WifiProfile>();

        var names = ProfileLine.Matches(output)
            .Select(m => m.Groups[1].Value.Trim())
            .Where(n => n.Length > 0)
            .Distinct()
            .ToList();

        using var gate = new SemaphoreSlim(4);
        var tasks = names.Select(async name =>
        {
            await gate.WaitAsync();
            try { return await GetProfileAsync(name); }
            finally { gate.Release(); }
        });

        return (await Task.WhenAll(tasks)).OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static async Task<WifiProfile> GetProfileAsync(string name)
    {
        string escaped = name.Replace("\"", "\\\"");
        var (_, output, _) = await ProcessRunner.RunAsync("netsh", $"wlan show profile name=\"{escaped}\" key=clear");

        var key = KeyLine.Match(output);
        var auth = AuthLine.Match(output);

        return new WifiProfile(
            name,
            key.Success ? key.Groups[1].Value.Trim() : null,
            auth.Success ? auth.Groups[1].Value.Trim() : "Unknown");
    }
}
