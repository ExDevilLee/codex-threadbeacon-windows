using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ThreadBeacon.App.Commands;
using ThreadBeacon.App.Formatting;
using ThreadBeacon.App.Localization;
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
    private string incidentDetailText = string.Empty;
    private string tokenText = "—";
    private TokenDetailViewModel? tokenDetails;
    private int subagentCount;
    private bool isSubagentExpanded;
    private bool isSubagentLoading;
    private ThreadRepositoryStatus subagentSourceStatus = ThreadRepositoryStatus.Healthy;
    private string durationText = string.Empty;
    private readonly Func<string, Task> toggleSubagents;
    private bool isPinned;
    private bool isFavorite;
    private bool isArchived;
    private AppLanguage language;

    public ThreadRowViewModel(
        ThreadSnapshot snapshot,
        DateTimeOffset now,
        Func<string, Task>? toggleSubagents = null,
        Action<string>? togglePin = null,
        Action<string>? ignore = null,
        Action<string>? toggleFavorite = null,
        AppLanguage language = AppLanguage.SimplifiedChinese)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Id = snapshot.Id;
        this.language = language;
        this.toggleSubagents = toggleSubagents ?? (_ => Task.CompletedTask);
        ToggleSubagentsCommand = new AsyncRelayCommand(() => this.toggleSubagents(Id));
        TogglePinCommand = new RelayCommand(() => togglePin?.Invoke(Id));
        IgnoreCommand = new RelayCommand(() => ignore?.Invoke(Id));
        ToggleFavoriteCommand = new RelayCommand(() => toggleFavorite?.Invoke(Id));
        Update(snapshot, now);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public ObservableCollection<SubagentRowViewModel> Subagents { get; } = [];

    public AsyncRelayCommand ToggleSubagentsCommand { get; }

    public RelayCommand TogglePinCommand { get; }

    public RelayCommand IgnoreCommand { get; }

    public RelayCommand ToggleFavoriteCommand { get; }

    public bool IsPinned
    {
        get => isPinned;
        private set
        {
            if (SetField(ref isPinned, value))
            {
                OnPropertyChanged(nameof(PinCommandLabel));
            }
        }
    }

    public string PinCommandLabel => AppLanguageText.PinCommand(language, IsPinned);

    public bool IsFavorite
    {
        get => isFavorite;
        private set
        {
            if (SetField(ref isFavorite, value))
            {
                OnPropertyChanged(nameof(FavoriteCommandLabel));
            }
        }
    }

    public string FavoriteCommandLabel => AppLanguageText.FavoriteCommand(language, IsFavorite);

    public bool IsArchived
    {
        get => isArchived;
        private set => SetField(ref isArchived, value);
    }

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

    public string IncidentDetailText
    {
        get => incidentDetailText;
        private set
        {
            if (SetField(ref incidentDetailText, value))
            {
                OnPropertyChanged(nameof(HasIncidentDetail));
            }
        }
    }

    public bool HasIncidentDetail => !string.IsNullOrEmpty(IncidentDetailText);

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
                OnPropertyChanged(nameof(SubagentToggleAccessibilityLabel));
            }
        }
    }

    public bool HasSubagents => SubagentCount > 0;

    public string SubagentCountText => HasSubagents
        ? SubagentCount.ToString(CultureInfo.InvariantCulture)
        : string.Empty;

    public string SubagentAccessibilityLabel => HasSubagents
        ? AppLanguageText.SubagentCount(language, SubagentCount)
        : string.Empty;

    public bool IsSubagentExpanded
    {
        get => isSubagentExpanded;
        private set
        {
            if (SetField(ref isSubagentExpanded, value))
            {
                OnPropertyChanged(nameof(SubagentToggleAccessibilityLabel));
                OnPropertyChanged(nameof(ShowSubagentRows));
                OnPropertyChanged(nameof(ShowSubagentPlaceholder));
            }
        }
    }

    public bool IsSubagentLoading
    {
        get => isSubagentLoading;
        private set
        {
            if (SetField(ref isSubagentLoading, value))
            {
                OnPropertyChanged(nameof(SubagentPlaceholderText));
            }
        }
    }

    public string SubagentToggleAccessibilityLabel => HasSubagents
        ? language is AppLanguage.SimplifiedChinese
            ? $"{(IsSubagentExpanded ? "收起" : "展开")} {SubagentAccessibilityLabel}"
            : $"{(IsSubagentExpanded ? "Collapse" : "Expand")} {SubagentAccessibilityLabel}"
        : string.Empty;

    public bool HasSubagentRows => Subagents.Count > 0;

    public bool ShowSubagentRows => IsSubagentExpanded && HasSubagentRows;

    public bool ShowSubagentPlaceholder => IsSubagentExpanded && !HasSubagentRows;

    public string SubagentPlaceholderText => IsSubagentLoading
        ? language is AppLanguage.SimplifiedChinese ? "正在读取 Subagent" : "Loading Subagents"
        : subagentSourceStatus is not ThreadRepositoryStatus.Healthy
            ? language is AppLanguage.SimplifiedChinese ? "Subagent 读取失败" : "Could not load Subagents"
            : language is AppLanguage.SimplifiedChinese ? "暂无可读取的 Subagent" : "No Subagents available";

    public string DurationText
    {
        get => durationText;
        private set => SetField(ref durationText, value);
    }

    public void Update(
        ThreadSnapshot snapshot,
        DateTimeOffset now,
        AppLanguage? languageOverride = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!StringComparer.Ordinal.Equals(Id, snapshot.Id))
        {
            throw new ArgumentException("A task row cannot change its thread ID.", nameof(snapshot));
        }

        AppLanguage nextLanguage = languageOverride ?? language;
        bool languageChanged = language != nextLanguage;
        language = nextLanguage;
        if (languageChanged)
        {
            OnPropertyChanged(nameof(PinCommandLabel));
            OnPropertyChanged(nameof(FavoriteCommandLabel));
            OnPropertyChanged(nameof(SubagentAccessibilityLabel));
            OnPropertyChanged(nameof(SubagentToggleAccessibilityLabel));
            OnPropertyChanged(nameof(SubagentPlaceholderText));
        }
        Title = snapshot.Title;
        Status = snapshot.Status;
        IsArchived = snapshot.IsArchived;
        StatusLabel = IsArchived
            ? language is AppLanguage.SimplifiedChinese ? "已归档" : "Archived"
            : GetStatusLabel(snapshot.Status, snapshot.ServiceIncident, language);
        StatusBrush = IsArchived ? IdleBrush : GetStatusBrush(snapshot.Status);
        IncidentDetailText = IsArchived
            ? string.Empty
            : GetIncidentDetail(snapshot.ServiceIncident, language);
        TokenText = TokenUsageFormatter.FormatCount(snapshot.TokenUsage?.TotalTokens);
        TokenDetails = snapshot.TokenUsage is null
                && string.IsNullOrWhiteSpace(snapshot.Model)
                && string.IsNullOrWhiteSpace(snapshot.ReasoningEffort)
            ? null
            : new TokenDetailViewModel(snapshot, language);
        SubagentCount = snapshot.SubagentCount;
        SetSubagentSourceStatus(snapshot.SubagentSourceStatus);
        ReconcileSubagents(snapshot.Subagents, now);
        DurationText = AppLanguageText.Duration(language, now - snapshot.StatusChangedAt);
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

    public void SetPinned(bool value) => IsPinned = value;

    public void SetFavorite(bool value) => IsFavorite = value;

    public void MarkSubagentLoadFailed()
    {
        SetSubagentSourceStatus(ThreadRepositoryStatus.Unavailable);
        SetSubagentExpanded(isExpanded: true, isLoading: false);
    }

    private void ReconcileSubagents(IReadOnlyList<SubagentSnapshot> snapshots, DateTimeOffset now)
    {
        int previousCount = Subagents.Count;
        for (int targetIndex = 0; targetIndex < snapshots.Count; targetIndex++)
        {
            SubagentSnapshot snapshot = snapshots[targetIndex];
            int existingIndex = FindSubagentIndex(snapshot.Id, targetIndex);
            if (existingIndex < 0)
            {
                Subagents.Insert(targetIndex, new SubagentRowViewModel(snapshot, now, language));
                continue;
            }

            SubagentRowViewModel row = Subagents[existingIndex];
            row.Update(snapshot, now, language);
            if (existingIndex != targetIndex)
            {
                Subagents.Move(existingIndex, targetIndex);
            }
        }

        while (Subagents.Count > snapshots.Count)
        {
            Subagents.RemoveAt(Subagents.Count - 1);
        }

        if (previousCount != Subagents.Count)
        {
            OnPropertyChanged(nameof(HasSubagentRows));
            OnPropertyChanged(nameof(ShowSubagentRows));
            OnPropertyChanged(nameof(ShowSubagentPlaceholder));
        }
    }

    private void SetSubagentSourceStatus(ThreadRepositoryStatus value)
    {
        if (subagentSourceStatus == value)
        {
            return;
        }

        subagentSourceStatus = value;
        OnPropertyChanged(nameof(SubagentPlaceholderText));
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

    private static string GetStatusLabel(
        ThreadStatus status,
        ServiceIncident? incident,
        AppLanguage language) => incident?.Phase switch
    {
        ServiceIncidentPhase.Retrying => language is AppLanguage.SimplifiedChinese ? "服务异常" : "Service issue",
        ServiceIncidentPhase.Failed => language is AppLanguage.SimplifiedChinese ? "服务失败" : "Service failed",
        _ => AppLanguageText.Status(language, status),
    };

    private static string GetIncidentDetail(ServiceIncident? incident, AppLanguage language)
    {
        if (incident is null)
        {
            return string.Empty;
        }

        var parts = new List<string>(2);
        if (incident.Kind is ServiceIncidentKind.ModelCapacity)
        {
            parts.Add(language is AppLanguage.SimplifiedChinese
                ? "模型容量不足"
                : "Model at capacity");
        }

        if (incident.Kind is ServiceIncidentKind.StreamDisconnected)
        {
            parts.Add(language is AppLanguage.SimplifiedChinese
                ? "连接中断"
                : "Connection interrupted");
        }

        if (incident.HttpStatusCode is int statusCode)
        {
            parts.Add($"HTTP {statusCode.ToString(CultureInfo.InvariantCulture)}");
        }

        if ((incident.Phase is ServiceIncidentPhase.Retrying
                || incident.Kind is ServiceIncidentKind.StreamDisconnected)
            && incident.RetryAttempt is int attempt
            && incident.RetryLimit is int limit)
        {
            string retry = language is AppLanguage.SimplifiedChinese ? "重试" : "Retry";
            parts.Add($"{retry} {attempt.ToString(CultureInfo.InvariantCulture)}/{limit.ToString(CultureInfo.InvariantCulture)}");
        }

        return string.Join(" · ", parts);
    }

    private static Brush GetStatusBrush(ThreadStatus status) => status switch
    {
        ThreadStatus.Error or ThreadStatus.NeedsAction => ErrorBrush,
        ThreadStatus.Warning => WarningBrush,
        ThreadStatus.Running => RunningBrush,
        ThreadStatus.JustCompleted => CompletedBrush,
        ThreadStatus.Idle => IdleBrush,
        _ => UnknownBrush,
    };

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
