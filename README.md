# EtherDNS

**EtherDNS** is a network management tool designed for **Windows 64-bit** systems using **.NET 9.0**. The application allows users to reset network configurations, set or remove DNS settings, and view Wi-Fi connection history. The tool provides a simple command-line interface for quickly managing DNS servers, optimizing web browsing and gaming performance, and troubleshooting network issues.

## Features

- **Reset Network**: Renew IP and flush DNS cache to resolve network problems.
- **Set DNS**: Choose and apply DNS servers for optimal performance.
- **Remove DNS Configuration**: Remove any manually configured DNS settings.
- **Wi-Fi History**: View recently connected Wi-Fi networks.
- **Developer Info**: Contact details and credits for the developer.

## Requirements

- **Windows 64-bit** (Other systems may not be supported).
- **.NET 9.0** (You need to have the .NET 9.0 runtime installed).

### Installing .NET 9.0

To install .NET 9.0, you can download it from the official site:

[Download .NET 9.0](https://dotnet.microsoft.com/download/dotnet)

After installing **.NET 9.0**, you can compile and run the project.

## Setup

1. **Clone the repository**:

   ```bash
   git clone https://github.com/your-username/EtherDNS.git
   cd EtherDNS
   ```

2. **Publish the project**:

   Since this is a **self-contained** application, use the following command to publish the application for **Windows x64**:

   ```bash
   dotnet publish -c Release --self-contained --runtime win-x64
   ```

   This will create a **Release** build of the project, packaged with all necessary dependencies for a **Windows 64-bit** environment.

3. **Run the application**:

   After publishing, you can navigate to the output folder (`bin\Release\net9.0\win-x64\publish\`) and run the application directly from there.

## How to Use

1. **Run the application**:
   After publishing, navigate to the folder where the application was published and execute the program:

   ```bash
   cd bin\Release\net9.0\win-x64\publish\
   etherdns.exe
   ```

2. **Choose an option**:
   - **Option 1**: Reset Network (Renew IP and Flush DNS)
   - **Option 2**: Set DNS (Choose DNS servers for your network)
   - **Option 3**: Remove DNS Configuration (Remove manually set DNS servers)
   - **Option 4**: Wi-Fi History (Show last connected Wi-Fi networks)
   - **Option 5**: Developer Info (Contact Info and Developer Credits)
   - **Option 0**: Exit (Close the program)

### Example

1. **Publish and run the application**:

   ```bash
   dotnet publish -c Release --self-contained --runtime win-x64
   ```

   Navigate to the output folder (`bin\Release\net9.0\win-x64\publish\`) and run:

   ```bash
   etherdns.exe
   ```

2. The program will display a menu to choose from:

   ```plaintext
   ======================================
   Select an option:
   1. Reset Network (Renew IP and Flush DNS)
   2. Set DNS (Choose DNS servers for your network)
   3. Remove DNS Configuration (Remove manually set DNS servers)
   4. Wi-Fi History (Show last connected Wi-Fi networks)
   5. Developer Info (Contact Info and Developer Credits)
   0. Exit (Close App)
   ======================================
   Choose an option:
   ```

3. If you choose option **2** (Set DNS), you will be presented with a list of available DNS servers to choose from:

   ```plaintext
   ======================================
   Select a DNS Service to Set:
   --------------------------------------
    + Active DNS: Cloudflare
   --------------------------------------
    ┌ 1. Google (Web)           : [8.8.8.8, 8.8.4.4]
    ├ 2. Cloudflare (Web)       : [1.1.1.1, 1.0.0.1]
    ├ 3. Shecan (Web-Game)      : [178.22.122.100, 185.51.200.2]
    ├ 4. Begzar (Web)           : [185.55.226.26, 185.55.225.25]
    ├ 5. Electro (Game)         : [78.157.42.100, 78.157.42.101]
    ├ 6. Radar Game (Game)      : [10.202.10.10, 10.202.10.11]
    ├ 7. 403.online (Game)      : [10.202.10.202, 10.202.10.102]
    ├ 8. Tci (Web-Game)         : [217.218.127.127, 217.218.155.155]
    ├ 9. AsiaTech (Web-Game)    : [185.98.113.113, 185.98.114.114]
    ├ 10. Shatel (Web-Game)     : [85.15.1.14, 85.15.1.15]
    ├ 11. Pishgaman (Web-Game)  : [5.202.100.100, 5.202.100.101]
    └ 12. Manually Set DNS      : [Enter custom DNS addresses]
    0. Back to Main Menu
   ======================================
   Choose a DNS service or option:
   ```

4. After selecting a DNS service, the tool will apply the settings and confirm the change.

## Code Overview

- **Reset Network**: Uses system commands to renew the IP address and flush the DNS cache.
- **Set DNS**: Modifies DNS settings via system commands.
- **Remove DNS Configuration**: Resets DNS settings to default values.
- **Wi-Fi History**: Retrieves and displays previously connected Wi-Fi networks.
- **Developer Info**: Displays developer contact information.

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details. The **MIT License** allows you to freely use, modify, and distribute the project, as long as the copyright notice and license text are included with any copies or substantial portions of the software.

## Support & Contributions

If you encounter any issues or have suggestions for improvement, please reach out via:

- [Telegram](https://t.me/DevUranium)
- [GitHub Issues](https://github.com/DevURANIUM/EtherDNS/issues)

### Contribution Guidelines

- Fork the repository and create a new branch.
- Submit a pull request with a detailed description of the changes.

## Donation Links

Support the project through donations:

- **BTC**: `bc1qcclcp574hnznm0nmdzzf0ta7366svjskttqks3`
- **TRON**: `TXJqhhwvkrTdnf5HReZf55hEzZuxjto3R4`
- **USDT-(TRC20)**: `TXJqhhwvkrTdnf5HReZf55hEzZuxjto3R4`
- **TON**: `UQAJH2N0pqpvC9YN841w5NH1dCN9Lakwkpjvoy7vXf-vfqgv`

---

This version reflects the use of **.NET** and includes the correct **dotnet publish** command for building a self-contained application for **Windows 64-bit**. It also provides clear instructions on how to publish, build, and run the application.
