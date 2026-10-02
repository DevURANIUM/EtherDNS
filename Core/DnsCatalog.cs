using System.Windows;
using System.Windows.Media;

namespace EtherDNS.Core;

public static class DnsCatalog
{
    // macOS system colors, each with a slightly lighter top for the classic app-icon sheen.
    static readonly (string A, string B)[] Palette =
    {
        ("#3D9BFF", "#0A84FF"), // blue
        ("#C97BF5", "#BF5AF2"), // purple
        ("#4ADB72", "#30D158"), // green
        ("#FFB13D", "#FF9F0A"), // orange
        ("#7C7AF0", "#5E5CE6"), // indigo
        ("#FF5C79", "#FF375F"), // pink
        ("#5FD8E8", "#40C8E0"), // cyan
    };

    /// <summary>Group key -> section title, in display order.</summary>
    public static readonly (string Key, string Title)[] Groups =
    {
        ("Iran",     "Iranian DNS"),
        ("Global",   "Global"),
        ("Privacy",  "Privacy"),
        ("AdBlock",  "Ad Block"),
        ("Security", "Security"),
        ("Family",   "Family Safe"),
    };

    // All entries verified to resolve before being listed.
    static readonly (string Name, string Group, string Tags, string Primary, string Secondary)[] Services =
    {
        // Iranian DNS: anti-sanction services first, then the ISPs
        ("Shecan",                 "Iran",     "Web · Gaming",          "178.22.122.100",  "185.51.200.2"),
        ("Begzar",                 "Iran",     "Web",                   "185.55.226.26",   "185.55.225.25"),
        ("Electro",                "Iran",     "Gaming",                "78.157.42.100",   "78.157.42.101"),
        ("Radar Game",             "Iran",     "Gaming",                "10.202.10.10",    "10.202.10.11"),
        ("403.online",             "Iran",     "Web · Gaming",          "10.202.10.202",   "10.202.10.102"),
        ("Dynx.pro",               "Iran",     "Web · Gaming",          "193.24.103.1",    "193.24.103.2"),
        ("Private IP",             "Iran",     "Web · Gaming",          "10.30.72.17",     "10.30.72.18"),
        ("Vanilla",                "Iran",     "Web · Gaming",          "10.139.177.21",   "10.139.177.22"),

        ("TCI",                    "Iran",      "Web · Gaming",          "5.200.200.200",   "217.218.127.127"),
        ("AsiaTech",               "Iran",      "Web · Gaming",          "185.98.113.113",  "185.98.114.114"),
        ("Shatel",                 "Iran",      "Web · Gaming",          "85.15.1.14",      "85.15.1.15"),
        ("Pishgaman",              "Iran",      "Web · Gaming",          "5.202.100.100",   "5.202.100.101"),
        ("Mobinnet",               "Iran",      "Web · Gaming",          "10.104.88.8",     "8.8.8.8"),
        ("ParsOnline",             "Iran",      "Web · Gaming",          "37.10.64.1",      "37.10.65.1"),
        ("Sabanet",                "Iran",      "Web · Gaming",          "89.40.90.100",    "188.158.158.158"),
        ("Taknet",                 "Iran",      "Web · Gaming",          "185.47.48.122",   "185.142.95.10"),
        ("Zi-Tel",                 "Iran",      "Web · Gaming",          "172.20.11.11",    "172.20.11.12"),

        // Global general-purpose resolvers
        ("Google",                 "Global",   "Fast · Reliable",       "8.8.8.8",         "8.8.4.4"),
        ("Cloudflare",             "Global",   "Fast · Private",        "1.1.1.1",         "1.0.0.1"),
        ("OpenDNS",                "Global",   "Reliable",              "208.67.222.222",  "208.67.220.220"),
        ("Level3",                 "Global",   "Reliable",              "4.2.2.1",         "4.2.2.2"),
        ("Level3 Alt",             "Global",   "Reliable",              "209.244.0.3",     "209.244.0.4"),
        ("Quad9 ECS",              "Global",   "CDN-optimized",         "9.9.9.11",        "149.112.112.11"),
        ("UltraDNS",               "Global",   "Reliable",              "156.154.70.1",    "156.154.71.1"),
        ("Gcore",                  "Global",   "Fast",                  "95.85.95.85",     "2.56.220.2"),
        ("Yandex",                 "Global",   "Reliable",              "77.88.8.8",       "77.88.8.1"),

        // Privacy-focused resolvers
        ("Quad9",                  "Privacy",  "No logs · Blocks malware", "9.9.9.9",      "149.112.112.112"),
        ("Quad9 Unfiltered",       "Privacy",  "No logs",               "9.9.9.10",        "149.112.112.10"),
        ("DNS.SB",                 "Privacy",  "No logs",               "185.222.222.222", "45.11.45.11"),
        ("NextDNS",                "Privacy",  "No logs",               "45.90.28.0",      "45.90.30.0"),
        ("Control D",              "Privacy",  "No logs",               "76.76.2.0",       "76.76.10.0"),
        ("AdGuard Unfiltered",     "Privacy",  "No logs",               "94.140.14.140",   "94.140.14.141"),

        // Ad / tracker blocking
        ("AdGuard",                "AdBlock",  "Ads · Trackers",        "94.140.14.14",    "94.140.15.15"),
        ("Control D Ad-Free",      "AdBlock",  "Ads · Trackers",        "76.76.2.2",       "76.76.10.2"),
        ("dnsforge",               "AdBlock",  "Ads · Trackers",        "176.9.93.198",    "176.9.1.117"),

        // Malware / phishing protection
        ("Cloudflare Security",    "Security", "Malware",               "1.1.1.2",         "1.0.0.2"),
        ("CleanBrowsing Security", "Security", "Malware · Phishing",    "185.228.168.9",   "185.228.169.9"),
        ("Comodo Secure",          "Security", "Malware · Phishing",    "8.26.56.26",      "8.20.247.20"),
        ("Control D Malware",      "Security", "Malware",               "76.76.2.1",       "76.76.10.1"),
        ("SafeDNS",                "Security", "Malware · Phishing",    "195.46.39.39",    "195.46.39.40"),
        ("Yandex Safe",            "Security", "Malware",               "77.88.8.88",      "77.88.8.2"),

        // Family-safe filtering (adult content)
        ("Cloudflare Family",      "Family",   "Adult · Malware",       "1.1.1.3",         "1.0.0.3"),
        ("AdGuard Family",         "Family",   "Adult · Ads",           "94.140.14.15",    "94.140.15.16"),
        ("OpenDNS FamilyShield",   "Family",   "Adult",                 "208.67.222.123",  "208.67.220.123"),
        ("CleanBrowsing Family",   "Family",   "Adult · Mixed content", "185.228.168.168", "185.228.169.168"),
        ("CleanBrowsing Adult",    "Family",   "Adult",                 "185.228.168.10",  "185.228.169.11"),
        ("Yandex Family",          "Family",   "Adult",                 "77.88.8.7",       "77.88.8.3"),
        ("Control D Family",       "Family",   "Adult · Malware",       "76.76.2.4",       "76.76.10.4"),
    };

    public static List<DnsItem> Create()
    {
        var titles = Groups.ToDictionary(g => g.Key, g => g.Title);
        var list = new List<DnsItem>();
        for (int i = 0; i < Services.Length; i++)
        {
            var s = Services[i];
            list.Add(new DnsItem(s.Name, s.Group, titles[s.Group], s.Tags, s.Primary, s.Secondary,
                Gradient(Palette[i % Palette.Length])));
        }
        return list;
    }

    static Brush Gradient((string A, string B) c)
    {
        var brush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString(c.A),
            (Color)ColorConverter.ConvertFromString(c.B),
            new Point(0, 0), new Point(0, 1));
        brush.Freeze();
        return brush;
    }
}
