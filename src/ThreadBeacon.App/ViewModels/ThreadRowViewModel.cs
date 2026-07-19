using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ThreadBeacon.App.Commands;
using ThreadBeacon.App.Formatting;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed class ThreadRowViewModel : INotifyPropertyChanged
{
    private static readonly Brush ErrorBrush = CreateBrush(0xE5, 0x48, 0x4D);
    private static readonly Brush WarningBrush = CreateBrush(0xF5, 0xA5, 0x24);
    private static readonly Brush RunningBrush = CreateBrush(0x30, 0xA4, 0x6C);
    private static readonly Brush CompletedBrush = CreateBrush(0x00, 0x7C, 0x91);
    private static readonly Brush IdleBrush = CreateBrush(0x8E, 0x8E, 0x93);
    private static readonly Brush UnknownBrush = CreateBrush(0xA0, 0xA0, 0xA6);

    private string title = string.Empty;
    private ThreadStatus status;
    private string statusLabel = string.Empty;
    private Brush statusBrush = UnknownBrush;
    private string tokenText = "—";
    private TokenDetailViewModel? tokenDetails;
    private int subagentCount;
    private bool isSubagentExpanded;
    private bool isSubagentLoading;
    private string durationText = string.Empty;
    private readonly Func<string, Task> toggleSubagents;

    public ThreadRowViewModel(
        ThreadSnapshot snapshot,
        DateTimeOffset now,
        Func<string, Task>? toggleSubagents = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Id = snapshot.Id;
        this.toggleSubagents = toggleSubagents ?? (_ => Task.CompletedTask);
        ToggleSubagentsCommand = new AsyncRelayCommand(() => this.toggleSubagents(Id));
        Update(snapshot, now);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public ObservableCollection<SubagentRowViewModel> Subagents { get; } = [];

    public AsyncRelayCommand ToggleSubagentsCommand { get; }

    public string Title
    {
        get => title;
        private set => SetField(ref title, value);
    }

    public ThreadStatus Status
    {
        get => status;
        private set => SetField(ref status, value);
    }

    public string StatusLabel
    {
        get => statusLabel;
        private set => SetField(ref statusLabel, value);
    }

    public Brush StatusBrush
    {
        get => statusBrush;
        private set => SetField(ref statusBrush, value);
    }

    public string TokenText
    {
        get => tokenText;
        private set => SetField(ref tokenText, value);
    }

    public TokenDetailViewModel? TokenDetails
    {
        get => tokenDetails;
        private set
        {
            if (SetField(ref tokenDetails, value))
            {
                OnPropertyChanged(nameof(HasTokenDetails));
            }
        }
    }

    public bool HasTokenDetails => TokenDetails is not null;

    public int SubagentCount
    {
        get => subagentCount;
        private set
        {
            value = Math.Max(0, value);
            if (SetField(ref subagentCount, value))
            {
                OnPropertyChanged(nameof(HasSubagents));
                OnPropertyChanged(nameof(SubagentCountText));
                OnPropertyChanged(nameof(SubagentAccessibilityLabel));
            }
        }
    }

    public bool HasSubagents => SubagentCount > 0;

    public string SubagentCountText => HasSubagents
        ? SubagentCount.ToString(CultureInfo.InvariantCulture)
        : string.Empty;

    public string SubagentAccessibilityLabel => HasSubagents
        ? $"{SubagentCountText} 个 Subagent"
        : string.Empty;

    public bool IsSubagentExpanded
    {
        get => isSubagentExpanded;
        private set
        {
            if (SetField(ref isSubagentExpanded, value))
            {
                OnPropertyChanged(nameof(SubagentToggleAccessibilityLabel));
            }
        }
    }

    public bool IsSubagentLoading
    {
        get => isSubagentLoading;
        private set => SetField(ref isSubagentLoading, value);
    }

    public string SubagentToggleAccessibilityLabel => HasSubagents
        ? $"{(IsSubagentExpanded ? "收起" : "展开")} {SubagentAccessibilityLabel}"
        : string.Empty;

    public string DurationText
    {
        get => durationText;
        private set => SetField(ref durationText, value);
    }

    public void Update(ThreadSnapshot snapshot, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!StringComparer.Ordinal.Equals(Id, snapshot.Id))
        {
            throw new ArgumentException("A task row cannot change its thread ID.", nameof(snapshot));
        }

        Title = snapshot.Title;
        Status = snapshot.Status;
        StatusLabel = GetStatusLabel(snapshot.Status);
        StatusBrush = GetStatusBrush(snapshot.Status);
        TokenText = TokenUsageFormatter.FormatCount(snapshot.TokenUsage?.TotalTokens);
        TokenDetails = snapshot.TokenUsage is null
            ? null
            : new TokenDetailViewModel(snapshot.TokenUsage);
        SubagentCount = snapshot.SubagentCount;
        ReconcileSubagents(snapshot.Subagents, now);
        DurationText = FormatDuration(now - snapshot.StatusChangedAt);
    }

    public void SetSubagentExpanded(bool isExpanded, bool isLoading)
    {
        IsSubagentExpanded = isExpanded;
        IsSubagentLoading = isExpanded && isLoading;
        if (!isExpanded)
        {
            Subagents.Clear();
        }
    }

    private void ReconcileSubagents(IReadOnlyList<SubagentSnapshot> snapshots, DateTimeOffset now)
    {
        for (int targetIndex = 0; targetIndex < snapshots.Count; targetIndex++)
        {
            SubagentSnapshot snapshot = snapshots[targetIndex];
            int existingIndex = FindSubagentIndex(snapshot.Id, targetIndex);
            if (existingIndex < 0)
            {
                Subagents.Insert(targetIndex, new SubagentRowViewModel(snapshot, now));
                continue;
            }

            SubagentRowViewModel row = Subagents[existingIndex];
            row.Update(snapshot, now);
            if (existingIndex != targetIndex)
            {
                Subagents.Move(existingIndex, targetIndex);
            }
        }

        while (Subagents.Count > snapshots.Count)
        {
            Subagents.RemoveAt(Subagents.Count - 1);
        }
    }

    private int FindSubagentIndex(string id, int startIndex)
    {
        for (int index = startIndex; index < Subagents.Count; index++)
        {
            if (StringComparer.Ordinal.Equals(Subagents[index].Id, id))
            {
                return index;
            }
        }

        return -1;
    }

    private static string GetStatusLabel(ThreadStatus status) => status switch
    {
        ThreadStatus.Error => "错误",
        ThreadStatus.NeedsAction => "待处理",
        ThreadStatus.Warning => "重试中",
        ThreadStatus.Running => "运行中",
        ThreadStatus.JustCompleted => "刚完成",
        ThreadStatus.Idle => "空闲",
        ThreadStatus.Unknown => "未知",
        _ => "未知",
    };

    private static Brush GetStatusBrush(ThreadStatus status) => status switch
    {
        ThreadStatus.Error or ThreadStatus.NeedsAction => ErrorBrush,
        ThreadStatus.Warning => WarningBrush,
        ThreadStatus.Running => RunningBrush,
        ThreadStatus.JustCompleted => CompletedBrush,
        ThreadStatus.Idle => IdleBrush,
        _ => UnknownBrush,
    };

    private static string FormatDuration(TimeSpan elapsed)
    {
        elapsed = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        return elapsed.TotalSeconds switch
        {
            < 60 => $"{Math.Floor(elapsed.TotalSeconds)}秒",
            < 3_600 => $"{Math.Floor(elapsed.TotalMinutes)}分",
            < 86_400 => $"{Math.Floor(elapsed.TotalHours)}时",
            _ => $"{Math.Floor(elapsed.TotalDays)}天",
        };
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
