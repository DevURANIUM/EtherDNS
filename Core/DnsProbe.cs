using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EtherDNS.Core;

/// <summary>
/// Measures real DNS response time by sending an A-record query over UDP/53.
/// More meaningful than ICMP ping, which many resolvers block.
/// </summary>
public static class DnsProbe
{
    const string ProbeHost = "google.com";

    public static async Task<int?> MeasureAsync(string server, int timeoutMs = 1500, int attempts = 3)
    {
        int? best = null;
        for (int i = 0; i < attempts; i++)
        {
            var ms = await QueryOnceAsync(server, timeoutMs);
            if (ms is int v && (best is null || v < best)) best = v;
            // Every query after a timeout will most likely time out as well.
            if (ms is null && i == 0) break;
        }
        return best;
    }

    static async Task<int?> QueryOnceAsync(string server, int timeoutMs)
    {
        if (!IPAddress.TryParse(server, out var ip)) return null;

        try
        {
            using var udp = new UdpClient(ip.AddressFamily);
            udp.Connect(ip, 53);

            ushort id = (ushort)Random.Shared.Next(1, ushort.MaxValue);
            byte[] query = BuildQuery(id, ProbeHost);

            using var cts = new CancellationTokenSource(timeoutMs);
            var sw = Stopwatch.StartNew();
            await udp.SendAsync(query, cts.Token);

            while (true)
            {
                var result = await udp.ReceiveAsync(cts.Token);
                var buf = result.Buffer;
                if (buf.Length >= 2 && ((buf[0] << 8) | buf[1]) == id)
                    return Math.Max(1, (int)Math.Round(sw.Elapsed.TotalMilliseconds));
            }
        }
        catch
        {
            return null;
        }
    }

    static byte[] BuildQuery(ushort id, string host)
    {
        var bytes = new List<byte>
        {
            (byte)(id >> 8), (byte)id,
            0x01, 0x00, // standard query, recursion desired
            0x00, 0x01, // QDCOUNT
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        foreach (var label in host.Split('.'))
        {
            bytes.Add((byte)label.Length);
            bytes.AddRange(Encoding.ASCII.GetBytes(label));
        }
        bytes.AddRange(new byte[] { 0x00, 0x00, 0x01, 0x00, 0x01 }); // root, QTYPE=A, QCLASS=IN
        return bytes.ToArray();
    }
}
