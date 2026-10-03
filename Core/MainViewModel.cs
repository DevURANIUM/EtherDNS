using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace EtherDNS.Core;

public enum ToastKind { Success, Error, Info }

public sealed class MainViewModel : ObservableObject
{
    public static MainViewModel Instance { get; } = new();

    public const string AppVersion = "2.1.0";

    MainViewModel()
    {
        DnsItems = new ObservableCollection<DnsItem>(DnsCatalog.Create());
        DnsView = CollectionViewSource.GetDefaultView(DnsItems);
        DnsView.Filter = FilterDns;
        ApplySort();

        WifiView = CollectionViewSource.GetDefaultView(WifiProfiles);
        WifiView.Filter = o => o is WifiProfile p &&
            (string.IsNullOrWhiteSpace(WifiSearch) || p.Name.Contains(WifiSearch.Trim(), StringComparison.OrdinalIgnoreCase));

        Fastest.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFastest));
        WifiProfiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(WifiCountText));
    }

    public event Action<string, ToastKind>? Toast;

    public string Version => AppVersion;
    public bool IsAdmin { get; } = AdminHelper.IsAdmin();

    // ---------- Adapters / status ----------

    public ObservableCollection<AdapterInfo> Adapters { get; } = new();

    AdapterInfo? _selectedAdapter;
    public AdapterInfo? SelectedAdapter
    {
        get => _selectedAdapter;
        set
        {
            // The ComboBox briefly pushes null while the list is rebuilt; ignore that.
            if (value == null && Adapters.Count > 0) return;
            if (Set(ref _selectedAdapter, value)) UpdateStatus();
        }
    }

    public bool HasAdapter => SelectedAdapter != null;
    public bool IsCustomDns => SelectedAdapter?.IsStaticDns == true;
    public string ActiveDnsText => SelectedAdapter?.DnsShortText ?? "—";
    public string ActiveDnsFull => SelectedAdapter?.DnsText ?? "—";
    public string DnsModeText => SelectedAdapter == null ? "—" : IsCustomDns ? "Static (manual)" : "Automatic (DHCP)";

    public string ActiveProviderName
    {
        get
        {
            var a = SelectedAdapter;
            if (a == null) return "No connection";
            var match = DnsItems.FirstOrDefault(d => d.IsActive);
            if (match != null) return match.Name;
            return a.IsStaticDns ? "Custom DNS" : "ISP / Router DNS";
        }
    }

    public void RefreshAdapters()
    {
        var keepId = SelectedAdapter?.Id;
        var list = NetworkService.GetAdapters();

        Adapters.Clear();
        foreach (var a in list) Adapters.Add(a);

        _selectedAdapter = null;
        SelectedAdapter = Adapters.FirstOrDefault(a => a.Id == keepId) ?? Adapters.FirstOrDefault();
        if (SelectedAdapter == null) UpdateStatus();
    }

    void UpdateStatus()
    {
        var a = SelectedAdapter;
        foreach (var d in DnsItems)
        {
            d.IsActive = a != null && a.IsStaticDns && a.Dns.Count > 0 &&
                         new HashSet<string>(a.Dns).SetEquals(new[] { d.Primary, d.Secondary });
        }

        OnPropertyChanged(nameof(HasAdapter));
        OnPropertyChanged(nameof(IsCustomDns));
        OnPropertyChanged(nameof(ActiveDnsText));
        OnPropertyChanged(nameof(ActiveDnsFull));
        OnPropertyChanged(nameof(DnsModeText));
        OnPropertyChanged(nameof(ActiveProviderName));
    }

    // ---------- Busy state ----------

    bool _isBusy;
    public bool IsBusy { get => _isBusy; private set => Set(ref _isBusy, value); }

    string _busyText = "";
    public string BusyText { get => _busyText; private set => Set(ref _busyText, value); }

    async Task RunBusyAsync(string text, Func<Task> action, string? successMessage)
    {
        if (IsBusy) return;
        BusyText = text;
        IsBusy = true;
        try
        {
            await action();
            if (successMessage != null) Toast?.Invoke(successMessage, ToastKind.Success);
        }
        catch (Exception ex)
        {
            Toast?.Invoke(FriendlyError(ex), ToastKind.Error);
        }
        finally
        {
            IsBusy = false;
            RefreshAdapters();
        }
    }

    string FriendlyError(Exception ex) =>
        ex is TimeoutException ? ex.Message :
        !IsAdmin ?"Administrator rights are required. Restart EtherDNS as administrator." : ex.Message;

    bool EnsureAdapter(out string iface)
    {
        iface = SelectedAdapter?.Name ?? "";
        if (iface.Length > 0) return true;
        Toast?.Invoke("No active network adapter was found.", ToastKind.Error);
        return false;
    }

    // ---------- DNS actions ----------

    public Task ApplyDnsAsync(DnsItem item) => ApplyDnsAsync(item.Name, item.Primary, item.Secondary);

    public Task ApplyDnsAsync(string name, string primary, string? secondary)
    {
        primary = primary.Trim();
        secondary = secondary?.Trim();

        if (!NetworkService.IsValidIPv4(primary) ||
            (!string.IsNullOrEmpty(secondary) && !NetworkService.IsValidIPv4(secondary)))
        {
            Toast?.Invoke("Please enter valid IPv4 addresses (e.g. 8.8.8.8).", ToastKind.Error);
            return Task.CompletedTask;
        }
        if (primary == secondary)
        {
            Toast?.Invoke("Primary and secondary DNS cannot be the same.", ToastKind.Error);
            return Task.CompletedTask;
        }
        if (!EnsureAdapter(out var iface)) return Task.CompletedTask;

        return RunBusyAsync($"Switching to {name}…",
            () => NetworkService.SetDnsAsync(iface, primary, secondary),
            $"{name} DNS is now active on {iface}");
    }

    public Task ResetDnsAsync()
    {
        if (!EnsureAdapter(out var iface)) return Task.CompletedTask;
        return RunBusyAsync($"Removing DNS from {iface}…",
            () => NetworkService.ResetDnsAsync(iface),
            $"Custom DNS removed from {iface} — back to automatic (DHCP)");
    }

    public Task RemoveDnsEverywhereAsync() =>
        RunBusyAsync("Removing DNS from all adapters…", async () =>
        {
            var (cleared, failed) = await NetworkService.ResetDnsOnAllAsync();
            if (failed.Count > 0)
                throw new InvalidOperationException($"Could not clear DNS on: {string.Join(", ", failed)}");

            Toast?.Invoke(cleared.Count == 0
                ? "No adapter had custom DNS — nothing to remove"
                : $"Custom DNS removed from {cleared.Count} adapter{(cleared.Count == 1 ? "" : "s")}: {string.Join(", ", cleared)}",
                ToastKind.Success);
        }, null);

    public Task FlushDnsAsync() =>
        RunBusyAsync("Flushing DNS cache…", NetworkService.FlushDnsAsync, "DNS cache flushed");

    public Task RenewIpAsync()
    {
        if (!EnsureAdapter(out var iface)) return Task.CompletedTask;
        return RunBusyAsync($"Renewing IP on {iface}…", () => NetworkService.RenewIpAsync(iface), "IP address renewed");
    }

    public Task FullResetAsync()
    {
        if (!EnsureAdapter(out var iface)) return Task.CompletedTask;
        return RunBusyAsync("Resetting network…", async () =>
        {
            BusyText = $"Renewing IP on {iface}…";
            await NetworkService.RenewIpAsync(iface);
            BusyText = "Flushing DNS cache…";
            await NetworkService.FlushDnsAsync();
        }, "Network reset complete");
    }

    // ---------- DNS list / benchmark ----------

    public ObservableCollection<DnsItem> DnsItems { get; }
    public ICollectionView DnsView { get; }
    public ObservableCollection<DnsItem> Fastest { get; } = new();
    public bool HasFastest => Fastest.Count > 0;

    string _search = "";
    public string Search { get => _search; set { if (Set(ref _search, value)) DnsView.Refresh(); } }

    string _category = "All";
    public string Category { get => _category; set { if (Set(ref _category, value)) DnsView.Refresh(); } }

    bool _sortBySpeed;
    public bool SortBySpeed
    {
        get => _sortBySpeed;
        set { if (Set(ref _sortBySpeed, value)) ApplySort(); }
    }

    bool _isBenchmarking;
    public bool IsBenchmarking { get => _isBenchmarking; private set => Set(ref _isBenchmarking, value); }

    bool FilterDns(object o)
    {
        if (o is not DnsItem d) return false;
        // "Game" cuts across groups; every other filter is a group key.
        if (Category == "Game" && !d.IsGame) return false;
        if (Category != "All" && Category != "Game" && d.Group != Category) return false;
        var q = Search.Trim();
        return q.Length == 0 ||
               d.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
               d.Primary.Contains(q) || d.Secondary.Contains(q);
    }

    void ApplySort()
    {
        // Grouped by section normally; one flat ranking when sorting by speed.
        using (DnsView.DeferRefresh())
        {
            DnsView.SortDescriptions.Clear();
            DnsView.GroupDescriptions.Clear();
            if (SortBySpeed) DnsView.SortDescriptions.Add(new SortDescription(nameof(DnsItem.SortKey), ListSortDirection.Ascending));
            else DnsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DnsItem.GroupTitle)));
        }
    }

    public async Task BenchmarkAsync()
    {
        if (IsBenchmarking) return;
        IsBenchmarking = true;
        Fastest.Clear();

        foreach (var d in DnsItems)
        {
            d.IsFastest = false;
            d.IsTesting = true;
        }

        using var gate = new SemaphoreSlim(8);
        await Task.WhenAll(DnsItems.Select(async d =>
        {
            await gate.WaitAsync();
            try
            {
                d.Latency = await DnsProbe.MeasureAsync(d.Primary);
                d.IsTested = true;
            }
            finally
            {
                d.IsTesting = false;
                gate.Release();
            }
        }));

        foreach (var d in DnsItems.Where(d => d.Latency != null).OrderBy(d => d.Latency).Take(3))
            Fastest.Add(d);

        if (Fastest.Count > 0)
        {
            Fastest[0].IsFastest = true;
            Toast?.Invoke($"Fastest DNS: {Fastest[0].Name} ({Fastest[0].Latency} ms)", ToastKind.Success);
        }
        else
        {
            Toast?.Invoke("No DNS server responded. Check your internet connection.", ToastKind.Error);
        }

        if (SortBySpeed) DnsView.Refresh();
        IsBenchmarking = false;
    }

    public async Task<int?> TestServerAsync(string ip) =>
        NetworkService.IsValidIPv4(ip) ? await DnsProbe.MeasureAsync(ip.Trim()) : null;

    // ---------- Wi-Fi ----------

    public ObservableCollection<WifiProfile> WifiProfiles { get; } = new();
    public ICollectionView WifiView { get; }

    string _wifiSearch = "";
    public string WifiSearch { get => _wifiSearch; set { if (Set(ref _wifiSearch, value)) WifiView.Refresh(); } }

    bool _isLoadingWifi;
    public bool IsLoadingWifi { get => _isLoadingWifi; private set => Set(ref _isLoadingWifi, value); }

    bool _wifiLoaded;
    public bool WifiEmpty => _wifiLoaded && WifiProfiles.Count == 0;
    public string WifiCountText => $"{WifiProfiles.Count} saved network{(WifiProfiles.Count == 1 ? "" : "s")}";

    public async Task LoadWifiAsync()
    {
        if (IsLoadingWifi) return;
        IsLoadingWifi = true;
        try
        {
            var profiles = await WifiService.GetProfilesAsync();
            WifiProfiles.Clear();
            foreach (var p in profiles) WifiProfiles.Add(p);
        }
        catch (Exception ex)
        {
            Toast?.Invoke(ex.Message, ToastKind.Error);
        }
        finally
        {
            _wifiLoaded = true;
            IsLoadingWifi = false;
            OnPropertyChanged(nameof(WifiEmpty));
        }
    }

    // ---------- Public IP info ----------

    PublicIpInfo? _publicIp;
    public PublicIpInfo? PublicIp
    {
        get => _publicIp;
        private set { if (Set(ref _publicIp, value)) OnPropertyChanged(nameof(HasPublicIp)); }
    }

    public bool HasPublicIp => PublicIp != null;

    bool _isLoadingIp;
    public bool IsLoadingIp { get => _isLoadingIp; private set => Set(ref _isLoadingIp, value); }

    string? _ipError;
    public string? IpError
    {
        get => _ipError;
        private set { if (Set(ref _ipError, value)) OnPropertyChanged(nameof(HasIpError)); }
    }

    public bool HasIpError => IpError != null;

    public async Task LoadIpInfoAsync()
    {
        if (IsLoadingIp) return;
        IsLoadingIp = true;
        IpError = null;
        try
        {
            PublicIp = await IpInfoService.GetAsync();
        }
        catch (Exception ex)
        {
            IpError = ex.Message;
        }
        finally
        {
            IsLoadingIp = false;
        }
    }

    // ---------- Updates (GitHub Releases) ----------

    UpdateInfo? _update;
    public UpdateInfo? AvailableUpdate
    {
        get => _update;
        private set { if (Set(ref _update, value)) NotifyUpdateState(); }
    }

    public bool HasUpdate => AvailableUpdate != null;

    bool _isCheckingUpdate;
    public bool IsCheckingUpdate
    {
        get => _isCheckingUpdate;
        private set { if (Set(ref _isCheckingUpdate, value)) NotifyUpdateState(); }
    }

    bool _isDownloadingUpdate;
    public bool IsDownloadingUpdate
    {
        get => _isDownloadingUpdate;
        private set { if (Set(ref _isDownloadingUpdate, value)) NotifyUpdateState(); }
    }

    double _updateProgress;
    public double UpdateProgress { get => _updateProgress; private set => Set(ref _updateProgress, value); }

    string _updateMessage = "Checks GitHub for new versions";
    public string UpdateMessage { get => _updateMessage; private set => Set(ref _updateMessage, value); }

    public bool CanCheckUpdate => !HasUpdate && !IsCheckingUpdate && !IsDownloadingUpdate;
    public bool CanInstallUpdate => HasUpdate && !IsDownloadingUpdate;
    public string UpdateHeadline => AvailableUpdate is { } h ? $"EtherDNS {h.Version.ToString(3)} is available" : "EtherDNS is up to date";
    public string UpdateButtonText => AvailableUpdate is { } u ? $"Update to {u.Version.ToString(3)}" : "Update";

    CancellationTokenSource? _downloadCts;

    void NotifyUpdateState()
    {
        OnPropertyChanged(nameof(HasUpdate));
        OnPropertyChanged(nameof(CanCheckUpdate));
        OnPropertyChanged(nameof(CanInstallUpdate));
        OnPropertyChanged(nameof(UpdateButtonText));
        OnPropertyChanged(nameof(UpdateHeadline));
    }

    /// <summary>Asks GitHub for the latest release. <paramref name="silent"/> = background check at startup.</summary>
    public async Task CheckForUpdatesAsync(bool silent)
    {
        if (IsCheckingUpdate || IsDownloadingUpdate) return;
        IsCheckingUpdate = true;
        UpdateMessage = "Checking for updates…";
        try
        {
            AvailableUpdate = await UpdateService.CheckAsync();
            if (AvailableUpdate is { } u)
            {
                UpdateMessage = $"Version {u.Version.ToString(3)} is available — you have {AppVersion}.";
                Toast?.Invoke($"EtherDNS {u.Version.ToString(3)} is available — open About to update", ToastKind.Info);
            }
            else
            {
                UpdateMessage = $"You're up to date — {AppVersion} is the latest version.";
                if (!silent) Toast?.Invoke("EtherDNS is up to date", ToastKind.Success);
            }
        }
        catch (Exception ex)
        {
            UpdateMessage = "Couldn't check for updates. Check your internet connection.";
            if (!silent) Toast?.Invoke("Update check failed: " + ex.Message, ToastKind.Error);
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    /// <summary>Downloads + verifies the installer, starts it and closes the app so it can be replaced.</summary>
    public async Task InstallUpdateAsync()
    {
        if (AvailableUpdate is not { } update || IsDownloadingUpdate) return;

        if (update.DownloadUrl == null)
        {
            // Release without an installer: send the user to the release page instead.
            AdminHelper.OpenUrl(update.PageUrl);
            return;
        }

        IsDownloadingUpdate = true;
        UpdateProgress = 0;
        UpdateMessage = $"Downloading EtherDNS {update.Version.ToString(3)}…";
        _downloadCts = new CancellationTokenSource();
        try
        {
            var progress = new Progress<double>(p =>
            {
                UpdateProgress = p;
                UpdateMessage = $"Downloading EtherDNS {update.Version.ToString(3)}…  {p:0}%";
            });
            var installer = await UpdateService.DownloadAsync(update, progress, _downloadCts.Token);

            UpdateMessage = "Installing — EtherDNS will restart automatically…";
            await Task.Delay(600);
            UpdateService.LaunchInstaller(installer);
            System.Windows.Application.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            UpdateMessage = "Download cancelled.";
        }
        catch (Exception ex)
        {
            UpdateMessage = "Update failed: " + ex.Message;
            Toast?.Invoke("Update failed: " + ex.Message, ToastKind.Error);
        }
        finally
        {
            IsDownloadingUpdate = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    public void CancelUpdateDownload() => _downloadCts?.Cancel();

    // ---------- Live DNS latency monitor ----------

    const int HistorySize = 30;
    public const double ChartWidth = 176;
    public const double ChartHeight = 40;

    readonly List<int?> _history = new();
    readonly System.Windows.Threading.DispatcherTimer _liveTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    bool _liveProbing;
    string? _liveServer;

    int? _liveLatency;
    public int? LiveLatency
    {
        get => _liveLatency;
        private set
        {
            Set(ref _liveLatency, value);
            OnPropertyChanged(nameof(LiveLatencyText));
            OnPropertyChanged(nameof(LiveLatencyLevel));
        }
    }

    public string LiveLatencyText => _history.Count == 0 ? "—" : LiveLatency is int ms ? $"{ms} ms" : "Timeout";

    public string LiveLatencyLevel =>
        _history.Count == 0 ? "none" :
        LiveLatency is null ? "fail" :
        LiveLatency < 60 ? "good" : LiveLatency < 150 ? "ok" : "bad";

    System.Windows.Media.PointCollection _latencyLine = new();
    public System.Windows.Media.PointCollection LatencyLine { get => _latencyLine; private set => Set(ref _latencyLine, value); }

    System.Windows.Media.PointCollection _latencyArea = new();
    public System.Windows.Media.PointCollection LatencyArea { get => _latencyArea; private set => Set(ref _latencyArea, value); }

    public void SetLiveMonitor(bool running)
    {
        if (running)
        {
            if (!_liveTimer.IsEnabled)
            {
                _liveTimer.Tick -= OnLiveTick;
                _liveTimer.Tick += OnLiveTick;
                _liveTimer.Start();
                OnLiveTick(null, EventArgs.Empty);
            }
        }
        else
        {
            _liveTimer.Stop();
        }
    }

    async void OnLiveTick(object? sender, EventArgs e)
    {
        var server = SelectedAdapter?.Dns.FirstOrDefault();
        if (server == null || _liveProbing || IsBusy) return;

        if (server != _liveServer)
        {
            _liveServer = server;
            _history.Clear();
        }

        _liveProbing = true;
        try
        {
            var ms = await DnsProbe.MeasureAsync(server, 1500, 1);
            if (server != _liveServer) return;

            _history.Add(ms);
            if (_history.Count > HistorySize) _history.RemoveAt(0);
            LiveLatency = ms;
            BuildChart();
        }
        finally
        {
            _liveProbing = false;
        }
    }

    void BuildChart()
    {
        var line = new System.Windows.Media.PointCollection();
        var area = new System.Windows.Media.PointCollection();
        if (_history.Count == 0)
        {
            LatencyLine = line;
            LatencyArea = area;
            return;
        }

        double max = Math.Max(60, _history.Where(v => v != null).Select(v => (double)v!.Value).DefaultIfEmpty(60).Max() * 1.15);
        double step = ChartWidth / (HistorySize - 1);
        int offset = HistorySize - _history.Count; // grow in from the right

        for (int i = 0; i < _history.Count; i++)
        {
            double value = _history[i] ?? max; // timeouts pin to the top
            double x = (offset + i) * step;
            double y = ChartHeight - 2 - (value / max) * (ChartHeight - 6);
            line.Add(new System.Windows.Point(x, y));
        }

        area.Add(new System.Windows.Point(line[0].X, ChartHeight));
        foreach (var p in line) area.Add(p);
        area.Add(new System.Windows.Point(line[^1].X, ChartHeight));

        line.Freeze();
        area.Freeze();
        LatencyLine = line;
        LatencyArea = area;
    }

    public void Notify(string message, ToastKind kind = ToastKind.Info) => Toast?.Invoke(message, kind);
}
