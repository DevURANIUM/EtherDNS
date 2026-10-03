using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EtherDNS.Core;

public sealed record UpdateInfo(
    Version Version,
    string Tag,
    string Title,
    string Notes,
    string PageUrl,
    string? DownloadUrl,
    string? AssetName,
    long Size,
    string? Sha256);

/// <summary>
/// Checks GitHub Releases for a newer version and installs it.
/// Convention: each release has a numeric tag (e.g. "2.1.0") and the Inno Setup installer attached as an .exe asset.
/// </summary>
public static class UpdateService
{
    public const string ReleasesPage = "https://github.com/DevURANIUM/EtherDNS/releases";
    const string LatestApi = "https://api.github.com/repos/DevURANIUM/EtherDNS/releases/latest";

    static readonly HttpClient Api = CreateClient(TimeSpan.FromSeconds(15));
    // Downloads can be large/slow; they are bounded by a CancellationToken instead.
    static readonly HttpClient Download = CreateClient(Timeout.InfiniteTimeSpan);

    static HttpClient CreateClient(TimeSpan timeout)
    {
        var client = new HttpClient { Timeout = timeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EtherDNS/" + MainViewModel.AppVersion);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public static Version CurrentVersion { get; } = ParseVersion(MainViewModel.AppVersion) ?? new Version(0, 0);

    /// <summary>"v2.1", "2.1.0", "EtherDNS 2.1.0-beta" → 2.1.0; null when the tag has no version number.</summary>
    public static Version? ParseVersion(string? tag)
    {
        var m = Regex.Match(tag ?? "", @"\d+(\.\d+){0,3}");
        if (!m.Success) return null;
        var parts = m.Value.Split('.').Select(int.Parse).ToList();
        while (parts.Count < 3) parts.Add(0);
        return new Version(parts[0], parts[1], parts[2], parts.Count > 3 ? parts[3] : 0);
    }

    /// <summary>The newest release if it is newer than this build, otherwise null.</summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        using var stream = await Api.GetStreamAsync(LatestApi);
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        string tag = Str(root, "tag_name") ?? "";
        var version = ParseVersion(tag);
        if (version == null || version <= Normalize(CurrentVersion)) return null;

        // Prefer an asset that looks like the installer, else any .exe.
        JsonElement? asset = null;
        if (root.TryGetProperty("assets", out var assets))
        {
            var exes = assets.EnumerateArray()
                .Where(a => (Str(a, "name") ?? "").EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var setup = exes.Where(a => (Str(a, "name") ?? "").Contains("setup", StringComparison.OrdinalIgnoreCase)).ToList();
            if (setup.Count > 0) asset = setup[0];
            else if (exes.Count > 0) asset = exes[0];
        }

        string? digest = asset is { } d ? Str(d, "digest") : null;
        return new UpdateInfo(
            version,
            tag,
            Str(root, "name") is { Length: > 0 } name ? name : tag,
            CleanNotes(Str(root, "body") ?? ""),
            Str(root, "html_url") ?? ReleasesPage,
            asset is { } a1 ? Str(a1, "browser_download_url") : null,
            asset is { } a2 ? Str(a2, "name") : null,
            asset is { } a3 && a3.TryGetProperty("size", out var size) && size.TryGetInt64(out var bytes) ? bytes : 0,
            digest != null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? digest[7..].ToLowerInvariant() : null);
    }

    /// <summary>Downloads the installer to %TEMP% and verifies its size and SHA-256 (when GitHub provides one).</summary>
    public static async Task<string> DownloadAsync(UpdateInfo update, IProgress<double> progress, CancellationToken ct)
    {
        if (update.DownloadUrl == null || update.AssetName == null)
            throw new InvalidOperationException("This release has no installer attached.");

        var dir = Path.Combine(Path.GetTempPath(), "EtherDNS-Update");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, Path.GetFileName(update.AssetName));

        // Unstable connections drop mid-download, so retry and resume from the bytes we already have.
        const int maxAttempts = 8;
        long received = 0;
        File.Delete(path);

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, update.DownloadUrl);
                if (received > 0) request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(received, null);

                using var response = await Download.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                // Server ignored the range: start over.
                if (received > 0 && response.StatusCode != System.Net.HttpStatusCode.PartialContent) received = 0;

                long total = update.Size > 0 ? update.Size : received + (response.Content.Headers.ContentLength ?? 0);

                await using var source = await response.Content.ReadAsStreamAsync(ct);
                await using var target = new FileStream(path, received > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write);
                var buffer = new byte[81920];
                int read;
                while ((read = await source.ReadAsync(buffer, ct)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), ct);
                    received += read;
                    if (total > 0) progress.Report(received * 100.0 / total);
                }
                break;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException && !ct.IsCancellationRequested && attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(2 * attempt, 8)), ct);
            }
        }

        var length = new FileInfo(path).Length;
        if (update.Size > 0 && length != update.Size)
            throw new InvalidDataException("The download was incomplete. Please try again.");

        if (update.Sha256 != null)
        {
            await using var file = File.OpenRead(path);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(file, ct)).ToLowerInvariant();
            if (hash != update.Sha256)
                throw new InvalidDataException("The downloaded file failed its integrity check. Please try again.");
        }

        return path;
    }

    /// <summary>
    /// Runs the installer silently over the current install. The app is already elevated, so no UAC prompt;
    /// the installer relaunches EtherDNS when it finishes (see [Run] in installer/EtherDNS.iss).
    /// </summary>
    public static void LaunchInstaller(string path)
    {
        Process.Start(new ProcessStartInfo(path, "/SILENT /SP- /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS")
        {
            UseShellExecute = true,
        });
    }

    static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

    static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    /// <summary>Turns release-note Markdown into readable plain text.</summary>
    static string CleanNotes(string markdown)
    {
        var lines = markdown.Replace("\r", "").Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => !l.StartsWith("```") && !l.StartsWith("---") && !l.StartsWith("!["))
            .Select(l => Regex.Replace(l, @"^#{1,6}\s*", ""))          // headings
            .Select(l => Regex.Replace(l, @"^\s*[-*]\s+", "•  "))       // bullets
            .Select(l => Regex.Replace(l, @"\*\*(.+?)\*\*", "$1"))      // bold
            .Select(l => Regex.Replace(l, @"`([^`]+)`", "$1"))          // inline code
            .Select(l => Regex.Replace(l, @"\[([^\]]+)\]\([^)]+\)", "$1")); // links
        var text = string.Join("\n", lines);
        return Regex.Replace(text, @"\n{3,}", "\n\n").Trim();
    }
}
