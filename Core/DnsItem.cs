using System.Windows.Media;

namespace EtherDNS.Core;

public sealed class DnsItem : ObservableObject
{
    public DnsItem(string name, string group, string groupTitle, string tags, string primary, string secondary, Brush accent)
    {
        Name = name;
        Group = group;
        GroupTitle = groupTitle;
        Category = tags;
        Primary = primary;
        Secondary = secondary;
        Accent = accent;
    }

    public string Name { get; }
    /// <summary>Group key (Iran, Global, Privacy, AdBlock, Security, Family).</summary>
    public string Group { get; }
    public string GroupTitle { get; }
    /// <summary>Short descriptive tags shown under the name.</summary>
    public string Category { get; }
    public string Primary { get; }
    public string Secondary { get; }
    public Brush Accent { get; }

    public string Initial => Name.Substring(0, 1).ToUpperInvariant();
    public bool IsGame => Category.Contains("Gaming");

    int? _latency;
    public int? Latency
    {
        get => _latency;
        set { if (Set(ref _latency, value)) NotifyLatency(); }
    }

    bool _isTested;
    public bool IsTested
    {
        get => _isTested;
        set { if (Set(ref _isTested, value)) NotifyLatency(); }
    }

    bool _isTesting;
    public bool IsTesting
    {
        get => _isTesting;
        set { if (Set(ref _isTesting, value)) NotifyLatency(); }
    }

    bool _isActive;
    public bool IsActive { get => _isActive; set => Set(ref _isActive, value); }

    bool _isFastest;
    public bool IsFastest { get => _isFastest; set => Set(ref _isFastest, value); }

    public string LatencyText =>
        IsTesting ? "Testing…" :
        !IsTested ? "Not tested" :
        Latency is int ms ? $"{ms} ms" : "Timeout";

    /// <summary>none | testing | good | ok | bad | fail — drives badge colors in XAML.</summary>
    public string LatencyLevel =>
        IsTesting ? "testing" :
        !IsTested ? "none" :
        Latency is null ? "fail" :
        Latency < 60 ? "good" :
        Latency < 150 ? "ok" : "bad";

    public int SortKey => !IsTested ? int.MaxValue - 1 : Latency ?? int.MaxValue;

    void NotifyLatency()
    {
        OnPropertyChanged(nameof(LatencyText));
        OnPropertyChanged(nameof(LatencyLevel));
        OnPropertyChanged(nameof(SortKey));
    }
}
