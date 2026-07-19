using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ThreadBeacon.App.Commands;
using ThreadBeacon.App.Formatting;
using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Sounds;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ThreadStatusLoader loader;
    private readonly ThreadRowCollection threadRows;
    private readonly HashSet<string> expandedThreadIds = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private readonly ICompletionNotificationObserver? completionObserver;
    private readonly IThreadListPreferenceStore? preferenceStore;
    private readonly TimeProvider timeProvider;
    private readonly AsyncRelayCommand refreshCommand;
    private readonly RelayCommand toggleFavoritesOnlyCommand;
    private ThreadListPreferences preferences;
    private IReadOnlyList<ThreadSnapshot> candidateSnapshots = [];
    private ThreadRepositoryStatus lastThreadSourceStatus = ThreadRepositoryStatus.Healthy;
    private SessionIndexStatus lastTitleSourceStatus = SessionIndexStatus.Healthy;
    private ThreadCountLabel threadCountLabel = ThreadCountFormatter.Format([]);
    private bool isRefreshing;
    private string sourceStatusText = "准备监听";
    private string updatedText = string.Empty;
    private bool hasSourceError;

    public MainWindowViewModel(ThreadStatusLoader loader, WindowPinState windowPin)
        : this(loader, windowPin, new MonitoringState())
    {
    }

    public MainWindowViewModel(
        ThreadStatusLoader loader,
        WindowPinState windowPin,
        MonitoringState monitoring,
        ICompletionNotificationObserver? completionObserver = null,
        IThreadListPreferenceStore? preferenceStore = null,
        TimeProvider? timeProvider = null)
    {
        this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
        WindowPin = windowPin ?? throw new ArgumentNullException(nameof(windowPin));
        Monitoring = monitoring ?? throw new ArgumentNullException(nameof(monitoring));
        this.completionObserver = completionObserver;
        this.preferenceStore = preferenceStore;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        preferences = preferenceStore?.Load() ?? new ThreadListPreferences();
        threadRows = new ThreadRowCollection(
            ToggleSubagentsAsync,
            TogglePin,
            IgnoreThread,
            ToggleFavorite);
        refreshCommand = new AsyncRelayCommand(
            () => RefreshAsync(RefreshNotificationPolicy.Baseline),
            () => !IsRefreshing);
        toggleFavoritesOnlyCommand = new RelayCommand(ToggleFavoritesOnly);
        Monitoring.PropertyChanged += OnMonitoringPropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ThreadRowViewModel> Threads => threadRows.Items;

    public ObservableCollection<IgnoredThreadRowViewModel> IgnoredThreads { get; } = [];

    public bool HasIgnoredThreads => IgnoredThreads.Count > 0;

    public bool ShowRestoreAllIgnored => IgnoredThreads.Count > 1;

    public RelayCommand RestoreAllIgnoredCommand => new(RestoreAllIgnored);

    public WindowPinState WindowPin { get; }

    public MonitoringState Monitoring { get; }

    public AsyncRelayCommand RefreshCommand => refreshCommand;

    public RelayCommand ToggleFavoritesOnlyCommand => toggleFavoritesOnlyCommand;

    public bool ShowsFavoritesOnly => preferences.ShowsFavoritesOnly;

    public string FavoritesFilterTooltip => ShowsFavoritesOnly
        ? "显示全部任务"
        : "仅显示收藏";

    public string EmptyStateIcon => ShowsFavoritesOnly ? "\uE734" : string.Empty;

    public string EmptyStateTitle => ShowsFavoritesOnly ? "暂无收藏任务" : "暂无任务数据";

    public string EmptyStateSubtitle => ShowsFavoritesOnly ? string.Empty : "等待 Codex 任务";

    public string ThreadCountText => threadCountLabel.DisplayText;

    public string ThreadCountAccessibilityLabel => threadCountLabel.AccessibilityLabel;

    public bool IsRefreshing
    {
        get => isRefreshing;
        private set
        {
            if (SetField(ref isRefreshing, value))
            {
                refreshCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusText
    {
        get
        {
            if (hasSourceError)
            {
                return sourceStatusText;
            }

            if (Monitoring.IsPaused)
            {
                return string.IsNullOrEmpty(updatedText)
                    ? "监听已暂停 · 尚未更新"
                    : "监听已暂停 · 上次更新";
            }

            return sourceStatusText;
        }
    }

    public string UpdatedText
    {
        get => updatedText;
        private set => SetField(ref updatedText, value);
    }

    public Visibility EmptyVisibility => Threads.Count == 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility ListVisibility => Threads.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public async Task RefreshAsync(
        RefreshNotificationPolicy policy = RefreshNotificationPolicy.Baseline)
    {
        if (!await refreshGate.WaitAsync(0))
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            var requestedExpandedIds = new HashSet<string>(expandedThreadIds, StringComparer.Ordinal);
            var includedIds = new HashSet<string>(preferences.PinnedThreadIds, StringComparer.Ordinal);
            includedIds.UnionWith(preferences.IgnoredRules.Keys);
            var favoriteIds = new HashSet<string>(
                preferences.FavoriteThreadIds,
                StringComparer.Ordinal);
            int recentLimit = Math.Min(int.MaxValue, 8 + preferences.IgnoredRules.Count);
            ThreadSnapshotLoadResult result = await Task.Run(
                () => loader.Load(new ThreadLoadRequest(
                    recentLimit,
                    includedIds,
                    requestedExpandedIds,
                    favoriteIds)));
            ThreadSnapshotLoadResult visibleResult = ApplyCandidates(result);
            completionObserver?.Observe(visibleResult.Threads, policy);
            sourceStatusText = GetStatusText(visibleResult);
            hasSourceError = visibleResult.ThreadSourceStatus is not ThreadRepositoryStatus.Healthy;
            updatedText = visibleResult.RefreshedAt.ToLocalTime().ToString("HH:mm:ss");
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(UpdatedText));
        }
        catch
        {
            foreach (ThreadRowViewModel row in Threads.Where(
                row => expandedThreadIds.Contains(row.Id)))
            {
                row.MarkSubagentLoadFailed();
            }

            sourceStatusText = "刷新失败";
            hasSourceError = true;
            OnPropertyChanged(nameof(StatusText));
        }
        finally
        {
            IsRefreshing = false;
            refreshGate.Release();
        }
    }

    public async Task ToggleSubagentsAsync(string threadId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);
        ThreadRowViewModel? row = Threads.FirstOrDefault(
            candidate => StringComparer.Ordinal.Equals(candidate.Id, threadId));
        if (row is null || !row.HasSubagents)
        {
            return;
        }

        if (expandedThreadIds.Remove(threadId))
        {
            row.SetSubagentExpanded(isExpanded: false, isLoading: false);
            return;
        }

        expandedThreadIds.Add(threadId);
        row.SetSubagentExpanded(isExpanded: true, isLoading: true);
        await RefreshAsync(RefreshNotificationPolicy.Baseline);
    }

    public void TogglePin(string threadId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);
        if (!preferences.PinnedThreadIds.Remove(threadId))
        {
            preferences.PinnedThreadIds.Add(threadId);
        }

        SaveAndReapplyPreferences();
    }

    public void ToggleFavorite(string threadId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);
        bool removed = preferences.FavoriteThreadIds.Remove(threadId);
        if (!removed)
        {
            preferences.FavoriteThreadIds.Add(threadId);
        }
        else if (candidateSnapshots.Any(snapshot =>
            snapshot.IsArchived && StringComparer.Ordinal.Equals(snapshot.Id, threadId)))
        {
            candidateSnapshots = candidateSnapshots
                .Where(snapshot => !StringComparer.Ordinal.Equals(snapshot.Id, threadId))
                .ToArray();
        }

        SaveAndReapplyPreferences();
    }

    public void ToggleFavoritesOnly()
    {
        preferences.ShowsFavoritesOnly = !preferences.ShowsFavoritesOnly;
        OnPropertyChanged(nameof(ShowsFavoritesOnly));
        OnPropertyChanged(nameof(FavoritesFilterTooltip));
        OnPropertyChanged(nameof(EmptyStateIcon));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateSubtitle));
        SaveAndReapplyPreferences();
    }

    public void IgnoreThread(string threadId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);
        preferences.PinnedThreadIds.Remove(threadId);
        preferences.IgnoredRules[threadId] = new IgnoredThreadRule(
            threadId,
            timeProvider.GetUtcNow(),
            ThreadIgnoreMode.UntilNextTurn);
        expandedThreadIds.Remove(threadId);
        SaveAndReapplyPreferences();
    }

    public void RestoreIgnoredThread(string threadId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);
        if (preferences.IgnoredRules.Remove(threadId))
        {
            SaveAndReapplyPreferences();
        }
    }

    public void RestoreAllIgnored()
    {
        if (preferences.IgnoredRules.Count == 0)
        {
            return;
        }

        preferences.IgnoredRules.Clear();
        SaveAndReapplyPreferences();
    }

    private ThreadSnapshotLoadResult ApplyCandidates(ThreadSnapshotLoadResult result)
    {
        candidateSnapshots = result.Threads;
        lastThreadSourceStatus = result.ThreadSourceStatus;
        lastTitleSourceStatus = result.TitleSourceStatus;
        ThreadListPreferences before = preferences.Clone();
        if (result.ThreadSourceStatus is ThreadRepositoryStatus.Healthy)
        {
            preferences.PinnedThreadIds.IntersectWith(candidateSnapshots.Select(thread => thread.Id));
        }

        ThreadListResult list = ApplyListPolicy(result.RefreshedAt);
        if (!PreferencesEqual(before, preferences))
        {
            preferenceStore?.Save(preferences);
        }

        return new ThreadSnapshotLoadResult(
            result.ThreadSourceStatus,
            result.TitleSourceStatus,
            list.VisibleSnapshots,
            result.RefreshedAt);
    }

    private ThreadListResult ApplyListPolicy(DateTimeOffset now)
    {
        ThreadListResult list = ThreadListPolicy.Evaluate(candidateSnapshots, preferences, 8);
        preferences = list.Preferences;
        expandedThreadIds.IntersectWith(list.VisibleSnapshots.Select(thread => thread.Id));
        threadRows.Reconcile(
            list.VisibleSnapshots,
            now,
            expandedThreadIds,
            preferences.PinnedThreadIds,
            preferences.FavoriteThreadIds);
        ReconcileIgnored();
        SetThreadCountLabel(ThreadCountFormatter.Format(
            list.VisibleSnapshots.Select(thread => thread.Status)));

        OnPropertyChanged(nameof(EmptyVisibility));
        OnPropertyChanged(nameof(ListVisibility));
        OnPropertyChanged(nameof(HasIgnoredThreads));
        OnPropertyChanged(nameof(ShowRestoreAllIgnored));
        return list;
    }

    private void ReconcileIgnored()
    {
        IgnoredThreads.Clear();
        foreach (string threadId in preferences.IgnoredRules.Keys.Order(StringComparer.Ordinal))
        {
            string title = candidateSnapshots.FirstOrDefault(snapshot => snapshot.Id == threadId)?.Title
                ?? string.Empty;
            IgnoredThreads.Add(new IgnoredThreadRowViewModel(
                threadId,
                title,
                RestoreIgnoredThread));
        }
    }

    private void SaveAndReapplyPreferences()
    {
        preferenceStore?.Save(preferences);
        DateTimeOffset now = timeProvider.GetUtcNow();
        ThreadListResult list = ApplyListPolicy(now);
        sourceStatusText = GetStatusText(new ThreadSnapshotLoadResult(
            lastThreadSourceStatus,
            lastTitleSourceStatus,
            list.VisibleSnapshots,
            now));
        OnPropertyChanged(nameof(StatusText));
    }

    private static bool PreferencesEqual(
        ThreadListPreferences left,
        ThreadListPreferences right) =>
        left.PinnedThreadIds.SetEquals(right.PinnedThreadIds)
        && left.FavoriteThreadIds.SetEquals(right.FavoriteThreadIds)
        && left.ShowsFavoritesOnly == right.ShowsFavoritesOnly
        && left.IgnoredRules.Count == right.IgnoredRules.Count
        && left.IgnoredRules.All(pair =>
            right.IgnoredRules.TryGetValue(pair.Key, out IgnoredThreadRule? rule)
            && rule == pair.Value);

    private void SetThreadCountLabel(ThreadCountLabel value)
    {
        if (threadCountLabel == value)
        {
            return;
        }

        threadCountLabel = value;
        OnPropertyChanged(nameof(ThreadCountText));
        OnPropertyChanged(nameof(ThreadCountAccessibilityLabel));
    }

    private static string GetStatusText(ThreadSnapshotLoadResult result)
    {
        if (result.ThreadSourceStatus is not ThreadRepositoryStatus.Healthy)
        {
            return result.ThreadSourceStatus switch
            {
                ThreadRepositoryStatus.Missing => "未找到 Codex 状态数据库",
                ThreadRepositoryStatus.Busy => "Codex 数据库繁忙",
                ThreadRepositoryStatus.Incompatible => "Codex 数据格式暂不兼容",
                _ => "Codex 数据暂不可用",
            };
        }

        string summary = $"监听中 · {result.Threads.Count} 个任务";
        return result.IsHealthy ? summary : $"{summary} · 部分数据降级";
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

    private void OnMonitoringPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MonitoringState.IsPaused))
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }
}
