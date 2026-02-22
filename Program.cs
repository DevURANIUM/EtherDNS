using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

class Program
{
    // DNS services (same items, but data-driven)
    static readonly List<DnsService> DnsServices = new List<DnsService>
    {
        new DnsService("1",  "Google (Web)",           "8.8.8.8",        "8.8.4.4"),
        new DnsService("2",  "Cloudflare (Web)",       "1.1.1.1",        "1.0.0.1"),
        new DnsService("3",  "Shecan (Web-Game)",      "178.22.122.100", "185.51.200.2"),
        new DnsService("4",  "Begzar (Web)",           "185.55.226.26",  "185.55.225.25"),
        new DnsService("5",  "Hostiran (Web)",         "172.29.0.100",   "172.29.2.100"),
        new DnsService("6",  "Electro (Game)",         "78.157.42.100",  "78.157.42.101"),
        new DnsService("7",  "Radar Game (Game)",      "10.202.10.10",   "10.202.10.11"),
        new DnsService("8",  "Dynx.pro (Web-Game)",    "193.24.103.1",   "193.24.103.2"),
        new DnsService("9",  "Private IP (Web-Game)",  "10.30.72.17",    "10.30.72.18"),
        new DnsService("10", "403.online (Web-Game)",  "10.202.10.202",  "10.202.10.102"),
        new DnsService("11", "Tci (Web-Game)",         "5.200.200.200",  "217.218.127.127"),
        new DnsService("12", "AsiaTech (Web-Game)",    "185.98.113.113", "185.98.114.114"),
        new DnsService("13", "Shatel (Web-Game)",      "85.15.1.14",     "85.15.1.15"),
        new DnsService("14", "Pishgaman (Web-Game)",   "5.202.100.100",  "5.202.100.101"),
        new DnsService("15", "Mobinnet (Web-Game)",    "10.104.88.8",    "8.8.8.8"),
        new DnsService("16", "ParsOnline (Web-Game)",  "37.10.64.1",     "37.10.65.1"),
        new DnsService("17", "Sabanet (Web-Game)",     "89.40.90.100",   "188.158.158.158"),
        new DnsService("18", "Taknet (Web-Game)",      "185.47.48.122",  "185.142.95.10"),
        new DnsService("19", "Zi-Tel (Web-Game)",      "172.20.11.11",   "172.20.11.12"),
    };

    static void Main(string[] args)
    {
        // Ensure the program is running with administrative privileges
        if (!IsRunningAsAdmin())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("This program requires Administrator privileges.");
            Thread.Sleep(2000);
            RestartAsAdmin();
            return;
        }

        while (true)
        {
            ShowMainMenu();
            string choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        ResetNetwork();
                        break;
                    case "2":
                        SetDNS();
                        break;
                    case "3":
                        RemoveDNS();
                        break;
                    case "4":
                        ShowWiFiHistory();
                        break;
                    case "5":
                        ShowDeveloperInfo();
                        break;
                    case "0":
                        return;  // Exit program
                    default:
                        Console.WriteLine("Invalid choice! Please choose a number between 0 and 5.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Thread.Sleep(2500);
            }
        }
    }

    static void ShowMainMenu()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("======================================");
        Console.WriteLine("Select an option:");
        Console.WriteLine("1. Reset Network (Renew IP and Flush DNS)");
        Console.WriteLine("2. Set DNS (Choose DNS servers for your network)");
        Console.WriteLine("3. Remove DNS Configuration (Remove manually set DNS servers)");
        Console.WriteLine("4. Wi-Fi History (Show last connected Wi-Fi networks)");
        Console.WriteLine("5. Developer Info (Contact Info and Developer Credits)"); // Developer Info
        Console.WriteLine("0. Exit (Close App)");
        Console.WriteLine("======================================");
        Console.Write("Choose an option: ");
    }

    static void ResetNetwork()
    {
        Console.Clear();
        Console.WriteLine("Renewing IP...");
        ExecuteCommandOrThrow("ipconfig /renew");
        Thread.Sleep(1000);

        Console.WriteLine("Flushing DNS...");
        ExecuteCommandOrThrow("ipconfig /flushdns");
        Thread.Sleep(1000);

        Console.WriteLine("Finish!");
        Thread.Sleep(3000);
        GoBackToMainMenu();
    }

    static void SetDNS()
    {
        Console.Clear();
        Console.WriteLine("======================================");
        Console.WriteLine("Select a DNS Service to Set:");
        Console.WriteLine("--------------------------------------");

        string iface = GetActiveInterfaceNameFallbackWiFi();

        string activeDNS = GetActiveDNS();
        Console.WriteLine($" + Active Interface: {iface}");
        Console.WriteLine($" + Active DNS: {GetActiveDNSName(activeDNS)}");

        Console.WriteLine("--------------------------------------");
        Console.WriteLine(" ┌ 1. Google (Web)           : [8.8.8.8, 8.8.4.4]");
        Console.WriteLine(" ├ 2. Cloudflare (Web)       : [1.1.1.1, 1.0.0.1]");
        Console.WriteLine(" ├ 3. Shecan (Web-Game)      : [178.22.122.100, 185.51.200.2]");
        Console.WriteLine(" ├ 4. Begzar (Web)           : [185.55.226.26, 185.55.225.25]");
        Console.WriteLine(" ├ 5. Hostiran (Web)         : [172.29.0.100, 172.29.2.100]");
        Console.WriteLine(" ├ 6. Electro (Game)         : [78.157.42.100, 78.157.42.101]");
        Console.WriteLine(" ├ 7. Radar Game (Game)      : [10.202.10.10, 10.202.10.11]");
        Console.WriteLine(" ├ 8. Dynx.pro (Web-Game)    : [193.24.103.1, 193.24.103.2]");
        Console.WriteLine(" ├ 9. Private IP (Web-Game)  : [10.30.72.17, 10.30.72.18]");
        Console.WriteLine(" ├ 10. 403.online (Web-Game) : [10.202.10.202, 10.202.10.102]");
        Console.WriteLine(" ├ 11. Tci (Web-Game)        : [5.200.200.200, 217.218.127.127]");
        Console.WriteLine(" ├ 12. AsiaTech (Web-Game)   : [185.98.113.113, 185.98.114.114]");
        Console.WriteLine(" ├ 13. Shatel (Web-Game)     : [85.15.1.14, 85.15.1.15]");
        Console.WriteLine(" ├ 14. Pishgaman (Web-Game)  : [5.202.100.100, 5.202.100.101]");
        Console.WriteLine(" ├ 15. Mobinnet (Web-Game)   : [10.104.88.8, 8.8.8.8]");
        Console.WriteLine(" ├ 16. ParsOnline (Web-Game) : [37.10.64.1, 37.10.65.1]");
        Console.WriteLine(" ├ 17. Sabanet (Web-Game)    : [89.40.90.100, 188.158.158.158]");
        Console.WriteLine(" ├ 18. Taknet (Web-Game)     : [185.47.48.122, 185.142.95.10]");
        Console.WriteLine(" ├ 19. Zi-Tel (Web-Game)     : [172.20.11.11, 172.20.11.12]");
        Console.WriteLine(" └ 20. Manually Set DNS      : [Enter custom DNS addresses]");
        Console.WriteLine("0. Back to Main Menu");

        Console.WriteLine("======================================");
        Console.Write("Choose a DNS service or option: ");

        string choice = Console.ReadLine();

        if (choice == "0") return;

        if (choice == "20")
        {
            ManuallySetDNS();
            return;
        }

        var service = DnsServices.FirstOrDefault(s => s.Id == choice);
        if (service == null)
        {
            Console.WriteLine("Invalid choice! Please choose a number between 0 and 20.");
            Thread.Sleep(1500);
            return;
        }

        SetDNS(service.Primary, service.Secondary);
    }

    static string GetActiveDNS()
    {
        string command = @"Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -match 'Wi-Fi|Ethernet' } | Select-Object -ExpandProperty ServerAddresses";
        string output = ExecutePowerShellCommand(command);

        if (string.IsNullOrEmpty(output))
            return "N/A";

        string[] dnsAddresses = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        List<string> validDNS = new List<string>();
        foreach (var dns in dnsAddresses)
        {
            var d = dns.Trim();
            if (IsValidIPv4(d))
                validDNS.Add(d);
        }

        if (validDNS.Count > 0)
            return string.Join(", ", validDNS.Distinct());

        return "N/A";
    }

    static string GetActiveDNSName(string activeDNS)
    {
        if (activeDNS == "N/A") return "N/A";

        var activeSet = new HashSet<string>(activeDNS.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries));

        foreach (var s in DnsServices)
        {
            var svcSet = new HashSet<string>(new[] { s.Primary, s.Secondary });
            if (activeSet.SetEquals(svcSet))
                return s.Title.Split('(')[0].Trim();
        }

        return activeDNS;
    }

    static bool IsValidIPv4(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out IPAddress ip) &&
               ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }

    static string ExecutePowerShellCommand(string command)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"" + command + "\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return (output ?? "").Trim();
        }
    }

    static void SetDNS(string primary, string secondary)
    {
        Console.Clear();

        if (!IsValidIPv4(primary) || !IsValidIPv4(secondary))
        {
            Console.WriteLine("Invalid DNS IPv4 address.");
            Thread.Sleep(2000);
            GoBackToDNSMenu();
            return;
        }

        if (primary == secondary)
        {
            Console.WriteLine("Error: DNS Primary and DNS Secondary cannot be the same.");
            Thread.Sleep(2000);
            GoBackToDNSMenu();
            return;
        }

        string iface = GetActiveInterfaceNameFallbackWiFi();

        Console.WriteLine($"Setting DNS to {primary} and {secondary}...");
        ExecuteCommandOrThrow($"netsh interface ipv4 set dnsservers name=\"{iface}\" source=static address={primary} validate=no");
        ExecuteCommandOrThrow($"netsh interface ipv4 add dnsservers name=\"{iface}\" address={secondary} index=2 validate=no");

        Console.WriteLine($"DNS Set to {primary} and {secondary} Successfully!");
        Thread.Sleep(3000);
        GoBackToDNSMenu();
    }

    static void ManuallySetDNS()
    {
        Console.Clear();
        Console.WriteLine("You can manually set DNS servers now.");

        string dns1, dns2;

        do
        {
            Console.Write("Enter DNS Primary (e.g. 8.8.8.8): ");
            dns1 = Console.ReadLine();
            Console.Write("Enter DNS Secondary (e.g. 8.8.4.4): ");
            dns2 = Console.ReadLine();

            if (dns1 == dns2)
            {
                Console.WriteLine("Error: DNS Primary and DNS Secondary cannot be the same. Please enter different DNS.");
            }

        } while (dns1 == dns2);

        SetDNS(dns1, dns2);
    }

    static void RemoveDNS()
    {
        Console.Clear();
        Console.WriteLine("Removing DNS Configuration...");

        string iface = GetActiveInterfaceNameFallbackWiFi();

        ExecuteCommandOrThrow($"netsh interface ipv4 set dnsservers name=\"{iface}\" source=dhcp");
        Console.WriteLine("DNS Configuration Removed Successfully!");
        Thread.Sleep(3000);
        GoBackToMainMenu();
    }

    static void ShowWiFiHistory()
    {
        Console.Clear();
        Console.WriteLine("======================================");
        Console.WriteLine("Showing Wi-Fi History:");
        Console.WriteLine("======================================");

        string output = ExecuteCommandAndGetOutput("netsh wlan show profiles");

        if (string.IsNullOrEmpty(output))
        {
            Console.WriteLine("No Wi-Fi profiles found.");
        }
        else
        {
            string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (line.Trim().StartsWith("All User Profile"))
                {
                    string profileName = line.Split(':')[1].Trim();
                    Console.WriteLine($"Profile: {profileName}");
                    ShowWiFiPassword(profileName);
                    Console.WriteLine("--------------------------------------");
                }
            }
        }

        Console.WriteLine("======================================");
        Console.WriteLine("Press any key to return to Main Menu...");

        Console.ReadKey();
        Console.Clear();
        ShowMainMenu();
    }

    static void ShowWiFiPassword(string profileName)
    {
        string output = ExecuteCommandAndGetOutput($"netsh wlan show profile name=\"{profileName}\" key=clear");

        if (output.Contains("Key Content"))
        {
            string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (line.Trim().StartsWith("Key Content"))
                {
                    string password = line.Split(':')[1].Trim();
                    Console.WriteLine($"Password: {password}");
                    return;
                }
            }
        }
        else
        {
            Console.WriteLine("Password: Not set");
        }
    }

    static string ExecuteCommandAndGetOutput(string command)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output ?? "";
        }
    }

    static void ExecuteCommandOrThrow(string command)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string msg = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                throw new Exception(msg.Trim());
            }
        }
    }

    static void ShowDeveloperInfo()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("======================================");
        Console.WriteLine("Developer Information:");
        Console.WriteLine("======================================");
        Console.WriteLine("TELEGRAM: t.me/DevRouter");
        Console.WriteLine("GitHub: Github.com/DevURANIUM");
        Console.WriteLine("Email: info@heydari.org");
        Console.WriteLine("Version: 1.2");
        Console.WriteLine();
        Console.WriteLine("If you liked this project, feel free to donate!");
        Console.WriteLine("======================================");
        Console.WriteLine("Press any key to return to the main menu...");
        Console.ReadKey();
        ShowMainMenu();
    }

    static void GoBackToMainMenu()
    {
        Console.WriteLine("Press any key to return to Main Menu...");
        Console.ReadKey();
        ShowMainMenu();
    }

    static void GoBackToDNSMenu()
    {
        Console.WriteLine("Press any key to return to DNS Settings...");
        Console.ReadKey();
        SetDNS();
    }

    static bool IsRunningAsAdmin()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    static void RestartAsAdmin()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = System.Reflection.Assembly.GetExecutingAssembly().Location,
            Verb = "runas",
            UseShellExecute = true
        };
        Process.Start(startInfo);
        Environment.Exit(0);
    }

    static string GetActiveInterfaceNameFallbackWiFi()
    {
        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n =>
                    n.OperationalStatus == OperationalStatus.Up &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .Select(n => new { Nic = n, Props = n.GetIPProperties() })
                .Where(x => x.Props.GatewayAddresses.Any(g => g.Address != null &&
                    g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                .Select(x => x.Nic)
                .FirstOrDefault();

            if (nic != null && !string.IsNullOrWhiteSpace(nic.Name))
                return nic.Name;
        }
        catch { }

        return "Wi-Fi"; // fallback like old version
    }

    class DnsService
    {
        public string Id;
        public string Title;
        public string Primary;
        public string Secondary;

        public DnsService(string id, string title, string primary, string secondary)
        {
            Id = id;
            Title = title;
            Primary = primary;
            Secondary = secondary;
        }
    }
}