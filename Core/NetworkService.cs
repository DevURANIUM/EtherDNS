using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Win32;

namespace EtherDNS.Core;

public sealed record AdapterInfo(
    string Id,
    string Name,
    string Description,
    string Kind,
    string IPv4,
    string Gateway,
    IReadOnlyList<string> Dns,
    long SpeedBps,
    bool IsStaticDns,
    string Mac)
{
    public bool HasGateway => Gateway != "—";
    public string Icon => Kind == "Wi-Fi" ? "" : "";
    public string DnsText => Dns.Count == 0 ? "—" : string.Join("  ·  ", Dns);

    /// <summary>At most two servers, then "+N" — long DHCP lists otherwise overflow the layout.</summary>
    public string DnsShortText =>
        Dns.Count == 0 ? "—" :
        Dns.Count <= 2 ? string.Join("  ·  ", Dns) :
        $"{Dns[0]}  ·  {Dns[1]}  +{Dns.Count - 2}";
    public string SpeedText =>
        SpeedBps <= 0 ? "—" :
        SpeedBps >= 1_000_000_000 ? $"{SpeedBps / 1e9:0.#} Gbps" :
        $"{SpeedBps / 1e6:0} Mbps";
}

public static class NetworkService
{
    public static List<AdapterInfo> GetAdapters()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .Select(ToInfo)
            .Where(a => a != null)
            .Select(a => a!)
            .OrderByDescending(a => a.HasGateway)
            .ThenBy(a => a.Kind == "Ethernet" ? 0 : a.Kind == "Wi-Fi" ? 1 : 2)
            .ToList();
    }

    static AdapterInfo? ToInfo(NetworkInterface n)
    {
        try
        {
            var props = n.GetIPProperties();
            var ipv4 = props.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();
            if (ipv4 == null) return null;

            var gateway = props.GatewayAddresses
                .FirstOrDefault(g => g.Address?.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString() ?? "—";

            var dns = props.DnsAddresses
                .Where(d => d.AddressFamily == AddressFamily.InterNetwork)
                .Select(d => d.ToString())
                .Distinct()
                .ToList();

            string kind = n.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Wireless80211 => "Wi-Fi",
                NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or NetworkInterfaceType.FastEthernetT => "Ethernet",
                _ => "Network",
            };

            var macBytes = n.GetPhysicalAddress().GetAddressBytes();
            string mac = macBytes.Length == 0 ? "—" : string.Join(":", macBytes.Select(b => b.ToString("X2")));

            return new AdapterInfo(n.Id, n.Name, n.Description, kind, ipv4, gateway, dns, n.Speed, IsStaticDns(n.Id), mac);
        }
        catch
        {
            return null;
        }
    }

    // A non-empty NameServer value means DNS was set manually; DHCP DNS lives in DhcpNameServer.
    static bool IsStaticDns(string interfaceId)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{interfaceId}");
            return !string.IsNullOrWhiteSpace(key?.GetValue("NameServer") as string);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidIPv4(string? value) =>
        IPAddress.TryParse(value?.Trim(), out var ip) &&
        ip.AddressFamily == AddressFamily.InterNetwork &&
        value!.Trim().Count(c => c == '.') == 3;

    public static async Task SetDnsAsync(string iface, string primary, string? secondary)
    {
        await ProcessRunner.RunOrThrowAsync("netsh",
            $"interface ipv4 set dnsservers name=\"{iface}\" source=static address={primary} register=primary validate=no");

        if (!string.IsNullOrWhiteSpace(secondary))
            await ProcessRunner.RunOrThrowAsync("netsh",
                $"interface ipv4 add dnsservers name=\"{iface}\" address={secondary} index=2 validate=no");

        await FlushDnsAsync();
    }

    /// <summary>Removes manually set DNS (IPv4 + IPv6) so the adapter goes back to DHCP-provided DNS.</summary>
    public static async Task ResetDnsAsync(string iface)
    {
        await ProcessRunner.RunOrThrowAsync("netsh", $"interface ipv4 set dnsservers name=\"{iface}\" source=dhcp");
        // IPv6 may be disabled on the adapter; that is fine.
        await ProcessRunner.RunAsync("netsh", $"interface ipv6 set dnsservers name=\"{iface}\" source=dhcp");
        await FlushDnsAsync();
    }

    /// <summary>Names of all adapters (connected or not) that have DNS set manually.</summary>
    public static List<string> GetAdaptersWithStaticDns() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && IsStaticDns(n.Id))
            .Select(n => n.Name)
            .ToList();

    /// <summary>Removes manual DNS from every adapter. Returns (cleared, failed) adapter names.</summary>
    public static async Task<(List<string> Cleared, List<string> Failed)> ResetDnsOnAllAsync()
    {
        var cleared = new List<string>();
        var failed = new List<string>();

        foreach (var name in GetAdaptersWithStaticDns())
        {
            var (code, _, _) = await ProcessRunner.RunAsync("netsh", $"interface ipv4 set dnsservers name=\"{name}\" source=dhcp");
            await ProcessRunner.RunAsync("netsh", $"interface ipv6 set dnsservers name=\"{name}\" source=dhcp");
            (code == 0 ? cleared : failed).Add(name);
        }

        await FlushDnsAsync();
        return (cleared, failed);
    }

    public static Task FlushDnsAsync() => ProcessRunner.RunOrThrowAsync("ipconfig", "/flushdns");

    // Renew only the chosen adapter: a bare "/renew" walks every DHCP adapter (VPN/TAP, Hyper-V…)
    // and blocks for minutes on any that has no DHCP server answering.
    public static async Task RenewIpAsync(string iface)
    {
        try
        {
            await ProcessRunner.RunOrThrowAsync("ipconfig", $"/renew \"{iface}\"", TimeSpan.FromSeconds(45));
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"No DHCP response on \"{iface}\" within 45 seconds. Check the connection and try again.");
        }
    }
}
