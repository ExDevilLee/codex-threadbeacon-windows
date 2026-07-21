using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ThreadBeacon.App.AutoRecovery;
using ThreadBeacon.App.Commands;
using ThreadBeacon.App.Formatting;
using ThreadBeacon.App.Localization;
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
    private readonly IAutoRecoveryObserver? autoRecoveryObserver;
    private readonly IThreadListPreferenceStore? preferenceStore;
    private readonly TimeProvider timeProvider;
    private readonly AsyncRelayCommand refreshCommand;
    private readonly RelayCommand toggleFavoritesOnlyCommand;
    private readonly DisplaySettingsViewModel displaySettings;
    private ThreadListPreferences preferences;
    private IReadOnlyList<ThreadSnapshot> candidateSnapshots = [];
    private ThreadRepositoryStatus lastThreadSourceStatus = ThreadRepositoryStatus.Healthy;
    private SessionIndexStatus lastTitleSourceStatus = SessionIndexStatus.Healthy;
    private ThreadCountLabel threadCountLabel = ThreadCountFormatter.Format([]);
    private bool isRefreshing;
    private string sourceStatusText = string.Empty;
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
        TimeProvider? timeProvider = null,
        DisplaySettingsViewModel? displaySettings = null,
        UpdateCheckViewModel? updateCheck = null,
        IAutoRecoveryObserver? autoRecoveryObserver = null)
    {
        this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
        WindowPin = windowPin ?? throw new ArgumentNullException(nameof(windowPin));
        Monitoring = monitoring ?? throw new ArgumentNullException(nameof(monitoring));
        this.completionObserver = completionObserver;
        this.autoRecoveryObserver = autoRecoveryObserver;
        this.preferenceStore = preferenceStore;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.displaySettings = displaySettings ?? new DisplaySettingsViewModel();
        UpdateCheck = updateCheck;
        sourceStatusText = this.displaySettings.EffectiveLanguage is AppLanguage.SimplifiedChinese
            ? "准备监听"
            : "Ready to monitor";
        preferences = preferenceStore?.Load() ?? new ThreadListPreferences();
        threadRows = new ThreadRowCollection(
            ToggleSubagentsAsync,
            TogglePin,
            IgnoreThread,
            ToggleFavorite,
            () => this.displaySettings.EffectiveLanguage);
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

    public DataSourceHealthViewModel DataSourceHealth { get; } = new();

    public UpdateCheckViewModel? UpdateCheck { get; }

    public AsyncRelayCommand RefreshCommand => refreshCommand;

    public RelayCommand ToggleFavoritesOnlyCommand => toggleFavoritesOnlyCommand;

    public bool ShowsFavoritesOnly => preferences.ShowsFavoritesOnly;

    public string FavoritesFilterTooltip => ShowsFavoritesOnly
        ? displaySettings.EffectiveLanguage is AppLanguage.SimplifiedChinese
            ? "显示全部任务"
            : "Show all tasks"
        : displaySettings.EffectiveLanguage is AppLanguage.SimplifiedChinese
            ? "仅显示收藏"
            : "Show favorites only";

    public string EmptyStateIcon => ShowsFavoritesOnly ? "\uE734" : string.Empty;

    public string EmptyStateTitle => displaySettings.EffectiveLanguage is AppLanguage.SimplifiedChinese
        ? ShowsFavoritesOnly ? "暂无收藏任务" : "暂无任务数据"
        : ShowsFavoritesOnly ? "No favorite tasks" : "No task data";

    public string EmptyStateSubtitle => ShowsFavoritesOnly
        ? string.Empty
        : displaySettings.EffectiveLanguage is AppLanguage.SimplifiedChinese
            ? "等待 Codex 任务"
            : "Waiting for Codex tasks";

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
                return displaySettings.EffectiveLanguage is AppLanguage.SimplifiedChinese
                    ? string.IsNullOrEmpty(updatedText)
                        ? "监听已暂停 · 尚未更新"
                        : "监听已暂停 · 上次更新"
                    : string.IsNullOrEmpty(updatedText)
                        ? "Monitoring paused · Not updated yet"
                        : "Monitoring paused · Last updated";
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
            int recentLimit = (int)Math.Min(
                int.MaxValue,
                (long)displaySettings.MaximumTaskCount + preferences.IgnoredRules.Count);
            ThreadSnapshotLoadResult result = await Task.Run(
                () => loader.Load(new ThreadLoadRequest(
                    recentLimit,
                    includedIds,
                    requestedExpandedIds,
                    favoriteIds)));
            if (result.Health.OverallStatus is OverallDataSourceHealth.Unavailable)
            {
                DataSourceHealth.Update(
                    PreserveLastSuccessfulRefresh(result.Health),
                    displaySettings.EffectiveLanguage);
                sourceStatusText = GetStatusText(result, displaySettings.EffectiveLanguage);
                hasSourceError = true;
                OnPropertyChanged(nameof(StatusText));
                return;
            }

            ThreadSnapshotLoadResult visibleResult = ApplyCandidates(result);
            DataSourceHealthReport successfulHealth =
                visibleResult.Health.WithLastSuccessfulRefresh(visibleResult.RefreshedAt);
            DataSourceHealth.Update(successfulHealth, displaySettings.EffectiveLanguage);
            completionObserver?.Observe(visibleResult.Threads, policy);
            if (autoRecoveryObserver is not null)
            {
                await autoRecoveryObserver.ObserveAsync(visibleResult.Threads, policy);
            }
            sourceStatusText = GetStatusText(visibleResult, displaySettings.EffectiveLanguage);
            hasSourceError = false;
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

            sourceStatusText = displaySettings.EffectiveLanguage is AppLanguage.SimplifiedChinese
                ? "刷新失败"
                : "Refresh failed";
            hasSourceError = true;
            DataSourceHealth.Update(
                CreateUnexpectedFailureHealth(),
                displaySettings.EffectiveLanguage);
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
        candidateSnapshots = result.Threads
            .Where(snapshot => !snapshot.IsArchived
                || preferences.FavoriteThreadIds.Contains(snapshot.Id))
            .ToArray();
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
            result.RefreshedAt,
            result.Health);
    }

    private ThreadListResult ApplyListPolicy(DateTimeOffset now)
    {
        ThreadListResult list = ThreadListPolicy.Evaluate(
            candidateSnapshots,
            preferences,
            displaySettings.MaximumTaskCount);
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
            list.VisibleSnapshots.Select(thread => thread.Status),
            displaySettings.EffectiveLanguage));

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
            now,
            DataSourceHealth.Report),
            displaySettings.EffectiveLanguage);
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

    private static string GetStatusText(
        ThreadSnapshotLoadResult result,
        AppLanguage language)
    {
        if (result.Health.OverallStatus is OverallDataSourceHealth.Unavailable)
        {
            return language is AppLanguage.SimplifiedChinese
                ? result.ThreadSourceStatus switch
                {
                    ThreadRepositoryStatus.Missing => "未找到 Codex 状态数据库",
                    ThreadRepositoryStatus.Busy => "Codex 数据库繁忙",
                    ThreadRepositoryStatus.Incompatible => "Codex 数据格式暂不兼容",
                    _ => "Codex 数据暂不可用",
                }
                : result.ThreadSourceStatus switch
            {
                ThreadRepositoryStatus.Missing => "Codex state database not found",
                ThreadRepositoryStatus.Busy => "Codex database is busy",
                ThreadRepositoryStatus.Incompatible => "Codex data format is not supported",
                _ => "Codex data unavailable",
            };
        }

        return AppLanguageText.MonitoringSummary(
            language,
            result.Threads.Count,
            !result.IsHealthy);
    }

    private DataSourceHealthReport PreserveLastSuccessfulRefresh(
        DataSourceHealthReport report) =>
        DataSourceHealth.Report.LastSuccessfulRefreshAt is DateTimeOffset refreshedAt
            ? report.WithLastSuccessfulRefresh(refreshedAt)
            : report;

    private DataSourceHealthReport CreateUnexpectedFailureHealth()
    {
        var report = new DataSourceHealthReport(
            DataSourceHealthStatus.Unavailable("Codex 任务数据库暂不可用"),
            DataSourceHealthStatus.NotUsed,
            DataSourceHealthStatus.NotUsed,
            DataSourceHealthStatus.NotUsed,
            0,
            0,
            null);
        return PreserveLastSuccessfulRefresh(report);
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
