using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;

class Program
{
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
        ExecuteCommand("ipconfig /renew");
        Thread.Sleep(1000);

        Console.WriteLine("Flushing DNS...");
        ExecuteCommand("ipconfig /flushdns");
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
        string activeDNS = GetActiveDNS();
        Console.WriteLine($" + Active DNS: {GetActiveDNSName(activeDNS)}");
        Console.WriteLine("--------------------------------------");
        Console.WriteLine(" ┌ 1. Google (Web)           : [8.8.8.8, 8.8.4.4]");
        Console.WriteLine(" ├ 2. Cloudflare (Web)       : [1.1.1.1, 1.0.0.1]");
        Console.WriteLine(" ├ 3. Shecan (Web-Game)      : [178.22.122.100, 185.51.200.2]");
        Console.WriteLine(" ├ 4. Begzar (Web)           : [185.55.226.26, 185.55.225.25]");
        Console.WriteLine(" ├ 5. Electro (Game)         : [78.157.42.100, 78.157.42.101]");
        Console.WriteLine(" ├ 6. Radar Game (Game)      : [10.202.10.10, 10.202.10.11]");
        Console.WriteLine(" ├ 7. 403.online (Game)      : [10.202.10.202, 10.202.10.102]");
        Console.WriteLine(" ├ 8. Tci (Web-Game)         : [217.218.127.127, 217.218.155.155]");
        Console.WriteLine(" ├ 9. AsiaTech (Web-Game)    : [185.98.113.113, 185.98.114.114]");
        Console.WriteLine(" ├ 10. Shatel (Web-Game)     : [85.15.1.14, 85.15.1.15]");
        Console.WriteLine(" ├ 11. Pishgaman (Web-Game)  : [5.202.100.100, 5.202.100.101]");
        Console.WriteLine(" └ 12. Manually Set DNS      : [Enter custom DNS addresses]");
        Console.WriteLine("0. Back to Main Menu");
        Console.WriteLine("======================================");
        Console.Write("Choose a DNS service or option: ");
        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1": SetGoogleDNS(); break;
            case "2": SetCloudflareDNS(); break;
            case "3": SetShecanDNS(); break;
            case "4": SetBegzarDNS(); break;
            case "5": SetElectroDNS(); break;
            case "6": SetRadarGameDNS(); break;
            case "7": SetDNS403(); break;
            case "8": SetTciDNS(); break;
            case "9": SetAsiaTechDNS(); break;
            case "10": SetShatelDNS(); break;
            case "11": SetPishgamanDNS(); break;
            case "12": ManuallySetDNS(); break;
            case "0": return;
            default:
                Console.WriteLine("Invalid choice! Please choose a number between 0 and 12.");
                break;
        }
    }

    static string GetActiveDNS()
    {
        string command = "Get-DnsClientServerAddress | Where-Object { $_.InterfaceAlias -match 'Wi-Fi|Ethernet' } | Select-Object -ExpandProperty ServerAddresses";
        string output = ExecutePowerShellCommand(command);

        if (string.IsNullOrEmpty(output))
        {
            return "N/A"; // If no DNS is active
        }

        string[] dnsAddresses = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        if (dnsAddresses.Length > 0)
        {
            List<string> validDNS = new List<string>();
            foreach (var dns in dnsAddresses)
            {
                if (IsValidIPv4(dns))
                {
                    validDNS.Add(dns);
                }
            }

            if (validDNS.Count > 0)
            {
                return string.Join(", ", validDNS);
            }
        }

        return "N/A"; // If no valid IPv4 DNS found
    }

    static string GetActiveDNSName(string activeDNS)
    {
        Dictionary<string, string[]> dnsServices = new Dictionary<string, string[]>
        {
            { "Google", new[] { "8.8.8.8", "8.8.4.4" } },
            { "Cloudflare", new[] { "1.1.1.1", "1.0.0.1" } },
            { "Shecan", new[] { "178.22.122.100", "185.51.200.2" } },
            { "Begzar", new[] { "185.55.226.26", "185.55.225.25" } },
            { "Electro", new[] { "78.157.42.100", "78.157.42.101" } },
            { "Radar Game", new[] { "10.202.10.10", "10.202.10.11" } },
            { "403.online", new[] { "10.202.10.202", "10.202.10.102" } },
            { "Tci", new[] { "217.218.127.127", "217.218.155.155" } },
            { "AsiaTech", new[] { "185.98.113.113", "185.98.114.114" } },
            { "Shatel", new[] { "85.15.1.14", "85.15.1.15" } },
            { "Pishgaman", new[] { "5.202.100.100", "5.202.100.101" } }
        };

        if (activeDNS == "N/A")
        {
            return "N/A";
        }

        var activeDNSSet = new HashSet<string>(activeDNS.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries));

        foreach (var service in dnsServices)
        {
            var serviceDNSSet = new HashSet<string>(service.Value);

            if (activeDNSSet.SetEquals(serviceDNSSet))
            {
                return service.Key; // نام سرویس
            }
        }

        return activeDNS;
    }

    static bool IsValidIPv4(string ipAddress)
    {
        return System.Net.IPAddress.TryParse(ipAddress, out System.Net.IPAddress ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }

    static string ExecutePowerShellCommand(string command)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("powershell.exe", "-Command \"" + command + "\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = Process.Start(startInfo);
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output.Trim();
    }

    static void SetGoogleDNS()
    {
        SetDNS("8.8.8.8", "8.8.4.4");
    }

    static void SetCloudflareDNS()
    {
        SetDNS("1.1.1.1", "1.0.0.1");
    }

    static void SetShecanDNS()
    {
        SetDNS("178.22.122.100", "185.51.200.2");
    }

    static void SetElectroDNS()
    {
        SetDNS("78.157.42.100", "78.157.42.101");
    }

    static void SetAsiaTechDNS()
    {
        SetDNS("185.98.113.113", "185.98.114.114");
    }

    static void SetDNS403()
    {
        SetDNS("10.202.10.202", "10.202.10.102");
    }

    static void SetRadarGameDNS()
    {
        SetDNS("10.202.10.10", "10.202.10.11");
    }

    static void SetBegzarDNS()
    {
        SetDNS("185.55.226.26", "185.55.225.25");
    }

    static void SetTciDNS()
    {
        SetDNS("217.218.127.127", "217.218.155.155");
    }

    static void SetPishgamanDNS()
    {
        SetDNS("5.202.100.100", "5.202.100.101");
    }

    static void SetShatelDNS()
    {
        SetDNS("85.15.1.14", "85.15.1.15");
    }

    static void SetDNS(string primary, string secondary)
    {
        Console.Clear();
        Console.WriteLine($"Setting DNS to {primary} and {secondary}...");
        
        ExecuteCommand($"netsh interface ip set dns name=\"Wi-Fi\" source=static addr={primary}");
        ExecuteCommand($"netsh interface ip add dns name=\"Wi-Fi\" addr={secondary} index=2");
        
        Console.WriteLine($"DNS Set to {primary} and {secondary} Successfully!");
        Thread.Sleep(3000);
        GoBackToDNSMenu(); // Return to DNS setup menu
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
        ExecuteCommand("netsh interface ip set dns name=\"Wi-Fi\" source=dhcp");
        Console.WriteLine("DNS Configuration Removed Successfully!");
        Thread.Sleep(3000);
        GoBackToMainMenu();  // Return to DNS setup menu
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
        Console.Clear(); // clear the screen to return to the main menu
        ShowMainMenu();  // Redisplay the main menu
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
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = Process.Start(startInfo);
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }

    static void ExecuteCommand(string command)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = Process.Start(startInfo);
        process.WaitForExit();
    }

    static void ShowDeveloperInfo()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("======================================");
        Console.WriteLine("Developer Information:");
        Console.WriteLine("======================================");
        Console.WriteLine("TELEGRAM: t.me/DevUranium");
        Console.WriteLine("GitHub: github.com/DevURANIUM");
        Console.WriteLine("Email: info@heydari.org");
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
        Console.ReadKey();  // Waiting for key press to return to DNS settings menu
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
            Verb = "runas"
        };
        Process.Start(startInfo);
        Environment.Exit(0);
    }
}
