using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace EtherDNS.Core;

public sealed record PublicIpInfo(
    string Ip,
    string? Country,
    string? CountryCode,
    string? Region,
    string? City,
    string? Isp,
    string? Org,
    string? Asn,
    string? Timezone,
    double? Lat,
    double? Lon,
    string Source)
{
    public string CountryCodeText => string.IsNullOrWhiteSpace(CountryCode) ? "??" : CountryCode!.ToUpperInvariant();
    public bool IsIPv6 => Ip.Contains(':');
    public string IpVersion => IsIPv6 ? "IPv6" : "IPv4";

    public string Location
    {
        get
        {
            var parts = new[] { City, Region, Country }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            var text = string.Join(", ", parts);
            return text.Length == 0 ? "Unknown location" : text;
        }
    }

    public bool HasCoordinates => Lat != null && Lon != null;
    public string CoordinatesText => HasCoordinates
        ? string.Create(CultureInfo.InvariantCulture, $"{Lat:0.####}, {Lon:0.####}")
        : "—";

    public string IspText => Isp ?? Org ?? "—";
    public string OrgText => Org ?? Isp ?? "—";
    public string AsnText => Asn ?? "—";
    public string TimezoneText => Timezone ?? "—";
    public string RegionText => Region ?? "—";
    public string CityText => City ?? "—";
    public string CountryText => Country ?? CountryCode ?? "—";
}

/// <summary>Looks up the public IP through several free providers, falling back if one is blocked.</summary>
public static class IpInfoService
{
    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EtherDNS/" + MainViewModel.AppVersion);
        return client;
    }

    public static async Task<PublicIpInfo> GetAsync()
    {
        var providers = new Func<Task<PublicIpInfo?>>[] { FromIpWho, FromIpInfo, FromIpApi };
        Exception? last = null;

        foreach (var provider in providers)
        {
            try
            {
                var info = await provider();
                if (info != null && !string.IsNullOrWhiteSpace(info.Ip)) return info;
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        throw new InvalidOperationException("Could not reach any IP lookup service. Check your internet connection.", last);
    }

    static async Task<JsonElement> GetJson(string url)
    {
        using var stream = await Http.GetStreamAsync(url);
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.Clone();
    }

    static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null
            ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString()) is { Length: > 0 } s ? s : null
            : null;

    static double? Num(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDouble()
            : null;

    static JsonElement Obj(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v : default;

    static async Task<PublicIpInfo?> FromIpWho()
    {
        var j = await GetJson("https://ipwho.is/");
        if (j.TryGetProperty("success", out var ok) && ok.ValueKind == JsonValueKind.False) return null;

        var conn = Obj(j, "connection");
        var asn = Str(conn, "asn");
        return new PublicIpInfo(
            Str(j, "ip") ?? "",
            Str(j, "country"), Str(j, "country_code"), Str(j, "region"), Str(j, "city"),
            Str(conn, "isp"), Str(conn, "org"),
            asn == null ? null : "AS" + asn,
            Str(Obj(j, "timezone"), "id"),
            Num(j, "latitude"), Num(j, "longitude"),
            "ipwho.is");
    }

    static async Task<PublicIpInfo?> FromIpInfo()
    {
        var j = await GetJson("https://ipinfo.io/json");

        // org looks like "AS12880 Information Technology Company"
        string? org = Str(j, "org"), asn = null;
        if (org != null && org.StartsWith("AS", StringComparison.OrdinalIgnoreCase) && org.Contains(' '))
        {
            asn = org[..org.IndexOf(' ')];
            org = org[(org.IndexOf(' ') + 1)..];
        }

        double? lat = null, lon = null;
        var loc = Str(j, "loc")?.Split(',');
        if (loc?.Length == 2 &&
            double.TryParse(loc[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var la) &&
            double.TryParse(loc[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lo))
        {
            lat = la;
            lon = lo;
        }

        var code = Str(j, "country");
        return new PublicIpInfo(
            Str(j, "ip") ?? "",
            CountryName(code), code, Str(j, "region"), Str(j, "city"),
            org, org, asn, Str(j, "timezone"), lat, lon,
            "ipinfo.io");
    }

    static async Task<PublicIpInfo?> FromIpApi()
    {
        // Free tier is HTTP only.
        var j = await GetJson("http://ip-api.com/json/");
        if (Str(j, "status") != "success") return null;

        var asField = Str(j, "as");
        return new PublicIpInfo(
            Str(j, "query") ?? "",
            Str(j, "country"), Str(j, "countryCode"), Str(j, "regionName"), Str(j, "city"),
            Str(j, "isp"), Str(j, "org"),
            asField?.Split(' ')[0],
            Str(j, "timezone"),
            Num(j, "lat"), Num(j, "lon"),
            "ip-api.com");
    }

    static string? CountryName(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        try { return new RegionInfo(code).EnglishName; }
        catch { return code; }
    }
}
