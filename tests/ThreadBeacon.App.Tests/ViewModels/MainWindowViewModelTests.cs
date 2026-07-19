using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Sounds;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MonitoringPause_ShowsLastUpdateWithoutBlockingManualRefresh()
    {
        var monitoring = new MonitoringState();
        MainWindowViewModel viewModel = CreateViewModel(
            monitoring,
            ThreadRepositoryStatus.Healthy);

        monitoring.ToggleCommand.Execute(null);
        Assert.Equal("监听已暂停 · 尚未更新", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.UpdatedText);

        await viewModel.RefreshAsync();

        Assert.Equal("监听已暂停 · 上次更新", viewModel.StatusText);
        Assert.Matches("^\\d{2}:\\d{2}:\\d{2}$", viewModel.UpdatedText);
    }

    [Fact]
    public async Task MonitoringPause_DoesNotHideSourceError()
    {
        var monitoring = new MonitoringState();
        MainWindowViewModel viewModel = CreateViewModel(
            monitoring,
            ThreadRepositoryStatus.Missing);

        await viewModel.RefreshAsync();
        monitoring.ToggleCommand.Execute(null);

        Assert.Equal("未找到 Codex 状态数据库", viewModel.StatusText);
    }

    [Fact]
    public async Task RefreshAsync_ForwardsExplicitNotificationPolicyAfterSuccessfulLoad()
    {
        var observer = new RecordingCompletionObserver();
        MainWindowViewModel viewModel = CreateViewModel(
            new MonitoringState(),
            ThreadRepositoryStatus.Healthy,
            observer);

        await viewModel.RefreshAsync(RefreshNotificationPolicy.Notify);

        Assert.Equal(RefreshNotificationPolicy.Notify, observer.LastPolicy);
        Assert.NotNull(observer.LastSnapshots);
    }

    [Fact]
    public async Task RefreshAsync_DefaultsToBaselineNotificationPolicy()
    {
        var observer = new RecordingCompletionObserver();
        MainWindowViewModel viewModel = CreateViewModel(
            new MonitoringState(),
            ThreadRepositoryStatus.Healthy,
            observer);

        await viewModel.RefreshAsync();

        Assert.Equal(RefreshNotificationPolicy.Baseline, observer.LastPolicy);
    }

    [Fact]
    public async Task RefreshAsync_UpdatesHeaderThreadCountAfterSuccessfulLoad()
    {
        var repository = new MutableThreadRepository(
            [Record("running-1"), Record("running-2"), Record("idle")]);
        MainWindowViewModel viewModel = CreateCountViewModel(
            repository,
            new Dictionary<string, ThreadStatus>
            {
                ["running-1"] = ThreadStatus.Running,
                ["running-2"] = ThreadStatus.Running,
                ["idle"] = ThreadStatus.Idle,
            });

        Assert.Equal("0/0", viewModel.ThreadCountText);

        await viewModel.RefreshAsync();

        Assert.Equal("2/3", viewModel.ThreadCountText);
        Assert.Equal(
            "2 个任务正在运行，共显示 3 个任务",
            viewModel.ThreadCountAccessibilityLabel);
    }

    [Fact]
    public async Task RefreshAsync_FailurePreservesLastSuccessfulHeaderThreadCount()
    {
        var repository = new MutableThreadRepository(
            [Record("running"), Record("idle")]);
        MainWindowViewModel viewModel = CreateCountViewModel(
            repository,
            new Dictionary<string, ThreadStatus>
            {
                ["running"] = ThreadStatus.Running,
                ["idle"] = ThreadStatus.Idle,
            });

        await viewModel.RefreshAsync();
        repository.ThrowOnLoad = true;

        await viewModel.RefreshAsync();

        Assert.Equal("1/2", viewModel.ThreadCountText);
        Assert.Equal(
            "1 个任务正在运行，共显示 2 个任务",
            viewModel.ThreadCountAccessibilityLabel);
    }

    [Fact]
    public async Task RefreshAsync_CoreUnavailablePreservesRowsHealthTimeAndNotifications()
    {
        var repository = new MutableThreadRepository([Record("running")]);
        var observer = new RecordingCompletionObserver();
        var timeProvider = new MutableTimeProvider(Now);
        var loader = new ThreadStatusLoader(
            repository,
            new HealthyTitleRepository(),
            new StatusRolloutParser(new Dictionary<string, ThreadStatus>
            {
                ["running"] = ThreadStatus.Running,
            }),
            timeProvider);
        var viewModel = new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState(),
            observer,
            timeProvider: timeProvider);

        await viewModel.RefreshAsync(RefreshNotificationPolicy.Notify);
        ThreadRowViewModel row = Assert.Single(viewModel.Threads);
        DataSourceHealthViewModel health = viewModel.DataSourceHealth;
        Assert.Equal(Now, health.Report.LastSuccessfulRefreshAt);
        Assert.Equal(1, observer.ObservationCount);

        timeProvider.Now = Now.AddSeconds(2);
        repository.Status = ThreadRepositoryStatus.Missing;
        await viewModel.RefreshAsync(RefreshNotificationPolicy.Notify);

        Assert.Same(row, Assert.Single(viewModel.Threads));
        Assert.Same(health, viewModel.DataSourceHealth);
        Assert.Equal(OverallDataSourceHealth.Unavailable, health.OverallStatus);
        Assert.Equal(Now, health.Report.LastSuccessfulRefreshAt);
        Assert.Equal(1, observer.ObservationCount);

        timeProvider.Now = Now.AddSeconds(4);
        repository.Status = ThreadRepositoryStatus.Healthy;
        await viewModel.RefreshAsync(RefreshNotificationPolicy.Notify);

        Assert.Same(health, viewModel.DataSourceHealth);
        Assert.Equal(Now.AddSeconds(4), health.Report.LastSuccessfulRefreshAt);
        Assert.Equal(2, observer.ObservationCount);
    }

    [Fact]
    public async Task RefreshAsync_SupplementalTaskFailureShowsDegradedSummary()
    {
        ThreadRecord recent = Record("recent");
        var preferences = new MemoryThreadListPreferenceStore(new ThreadListPreferences(
            pinnedThreadIds: ["included"]));
        var loader = new ThreadStatusLoader(
            new SupplementalFailureThreadRepository(recent),
            new HealthyTitleRepository(),
            new StatusRolloutParser(new Dictionary<string, ThreadStatus>
            {
                ["recent"] = ThreadStatus.Running,
            }),
            new FixedTimeProvider(Now));
        var viewModel = new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState(),
            preferenceStore: preferences,
            timeProvider: new FixedTimeProvider(Now));

        await viewModel.RefreshAsync();

        Assert.Equal("监听中 · 1 个任务 · 部分数据降级", viewModel.StatusText);
        Assert.Equal(OverallDataSourceHealth.Degraded, viewModel.DataSourceHealth.OverallStatus);
    }

    [Fact]
    public async Task RefreshAsync_LoaderFailureDoesNotInvokeNotificationObserver()
    {
        var observer = new RecordingCompletionObserver();
        var loader = new ThreadStatusLoader(
            new ThrowingThreadRepository(),
            new HealthyTitleRepository(),
            new UnusedRolloutParser());
        var viewModel = new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState(),
            observer);

        await viewModel.RefreshAsync(RefreshNotificationPolicy.Notify);

        Assert.Null(observer.LastPolicy);
    }

    [Fact]
    public async Task ToggleSubagentsAsync_LoadsExpandedParentAndCollapsesImmediately()
    {
        var repository = new ExpandableThreadRepository();
        var loader = new ThreadStatusLoader(
            repository,
            new HealthyTitleRepository(),
            new StatusRolloutParser(new Dictionary<string, ThreadStatus>
            {
                ["parent"] = ThreadStatus.Running,
                ["child"] = ThreadStatus.Idle,
            }),
            new FixedTimeProvider(Now));
        var viewModel = new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState());
        await viewModel.RefreshAsync();

        await viewModel.ToggleSubagentsAsync("parent");

        Assert.Equal(["parent"], repository.LastRequestedParentIds);
        ThreadRowViewModel parent = Assert.Single(viewModel.Threads);
        Assert.True(parent.IsSubagentExpanded);
        Assert.Equal("child", Assert.Single(parent.Subagents).Id);

        await viewModel.ToggleSubagentsAsync("parent");

        Assert.False(parent.IsSubagentExpanded);
        Assert.Empty(parent.Subagents);
        Assert.Equal(1, repository.SubagentLoadCount);
    }

    [Fact]
    public async Task ToggleSubagentsAsync_LoaderExceptionStopsLoadingAndShowsFailure()
    {
        var repository = new ExpandableThreadRepository();
        var loader = new ThreadStatusLoader(
            repository,
            new HealthyTitleRepository(),
            new StatusRolloutParser(new Dictionary<string, ThreadStatus>
            {
                ["parent"] = ThreadStatus.Running,
                ["child"] = ThreadStatus.Idle,
            }),
            new FixedTimeProvider(Now));
        var viewModel = new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState());
        await viewModel.RefreshAsync();
        repository.ThrowOnLoadRecent = true;

        await viewModel.ToggleSubagentsAsync("parent");

        ThreadRowViewModel parent = Assert.Single(viewModel.Threads);
        Assert.True(parent.IsSubagentExpanded);
        Assert.False(parent.IsSubagentLoading);
        Assert.Equal("Subagent 读取失败", parent.SubagentPlaceholderText);
    }

    [Fact]
    public async Task RefreshAsync_AppliesPinnedAndIgnoredPreferencesBeforeNotifications()
    {
        ThreadRecord[] records = [Record("recent"), Record("pinned"), Record("ignored")];
        var repository = new PreferenceThreadRepository(records);
        var preferenceStore = new MemoryThreadListPreferenceStore(new ThreadListPreferences(
            pinnedThreadIds: ["pinned"],
            ignoredRules: new Dictionary<string, IgnoredThreadRule>(StringComparer.Ordinal)
            {
                ["ignored"] = new("ignored", Now, ThreadIgnoreMode.UntilNextTurn),
            }));
        var observer = new RecordingCompletionObserver();
        var loader = new ThreadStatusLoader(
            repository,
            new HealthyTitleRepository(),
            new StatusRolloutParser(records.ToDictionary(
                record => record.Id,
                _ => ThreadStatus.Running,
                StringComparer.Ordinal)),
            new FixedTimeProvider(Now));
        var viewModel = new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState(),
            observer,
            preferenceStore,
            new FixedTimeProvider(Now));

        await viewModel.RefreshAsync(RefreshNotificationPolicy.Notify);

        Assert.Equal(9, repository.LastRecentLimit);
        Assert.True(repository.LastIncludedIds!.SetEquals(["ignored", "pinned"]));
        Assert.Equal(["pinned", "recent"], viewModel.Threads.Select(row => row.Id));
        Assert.Equal(["pinned", "recent"], observer.LastSnapshots!.Select(row => row.Id));
        Assert.True(viewModel.HasIgnoredThreads);
        Assert.Equal("ignored", Assert.Single(viewModel.IgnoredThreads).Id);
    }

    [Fact]
    public async Task RefreshAsync_UsesConfiguredMaximumTaskCount()
    {
        ThreadRecord[] records = Enumerable.Range(1, 20)
            .Select(index => Record($"task-{index:D2}"))
            .ToArray();
        var repository = new PreferenceThreadRepository(records);
        var displaySettings = new DisplaySettingsViewModel(
            new MemoryDisplaySettingsStore(new DisplaySettings(maximumTaskCount: 12)));
        var loader = new ThreadStatusLoader(
            repository,
            new HealthyTitleRepository(),
            new StatusRolloutParser(records.ToDictionary(
                record => record.Id,
                _ => ThreadStatus.Running,
                StringComparer.Ordinal)),
            new FixedTimeProvider(Now));
        var viewModel = new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState(),
            displaySettings: displaySettings);

        await viewModel.RefreshAsync();

        Assert.Equal(12, repository.LastRecentLimit);
        Assert.Equal(12, viewModel.Threads.Count);
    }

    [Fact]
    public async Task IgnoreAndRestore_UpdateRowsAndPersistImmediately()
    {
        ThreadRecord[] records = [Record("task")];
        var preferenceStore = new MemoryThreadListPreferenceStore(new ThreadListPreferences());
        MainWindowViewModel viewModel = CreatePreferenceViewModel(records, preferenceStore);
        await viewModel.RefreshAsync();

        viewModel.IgnoreThread("task");

        Assert.Empty(viewModel.Threads);
        Assert.True(viewModel.HasIgnoredThreads);
        Assert.Contains("task", preferenceStore.LastSaved!.IgnoredRules.Keys);
        Assert.Contains("0", viewModel.StatusText);

        viewModel.RestoreIgnoredThread("task");

        Assert.Equal("task", Assert.Single(viewModel.Threads).Id);
        Assert.False(viewModel.HasIgnoredThreads);
        Assert.Contains("1", viewModel.StatusText);
    }

    [Fact]
    public async Task RefreshAsync_KeepsRecoveryEntryForMissingIgnoredCandidate()
    {
        var preferenceStore = new MemoryThreadListPreferenceStore(new ThreadListPreferences(
            ignoredRules: new Dictionary<string, IgnoredThreadRule>(StringComparer.Ordinal)
            {
                ["missing-task-id"] = new(
                    "missing-task-id",
                    Now,
                    ThreadIgnoreMode.UntilNextTurn),
            }));
        MainWindowViewModel viewModel = CreatePreferenceViewModel([], preferenceStore);

        await viewModel.RefreshAsync();

        IgnoredThreadRowViewModel row = Assert.Single(viewModel.IgnoredThreads);
        Assert.Equal("missing-task-id", row.Id);
        Assert.Equal("任务 missing-", row.Title);
    }

    [Fact]
    public async Task RefreshAsync_ForwardsFavoriteIdsThroughArchivedCapableRequest()
    {
        ThreadRecord[] records = [Record("favorite"), Record("other")];
        var repository = new PreferenceThreadRepository(records);
        var preferenceStore = new MemoryThreadListPreferenceStore(new ThreadListPreferences(
            favoriteThreadIds: ["favorite", "missing"]));
        MainWindowViewModel viewModel = CreatePreferenceViewModel(records, preferenceStore, repository);

        await viewModel.RefreshAsync();

        Assert.True(repository.LastFavoriteIds!.SetEquals(["favorite", "missing"]));
    }

    [Fact]
    public async Task ToggleFavorite_PersistsWithoutChangingAllTasksOrder()
    {
        ThreadRecord[] records = [Record("b"), Record("a")];
        var preferenceStore = new MemoryThreadListPreferenceStore(new ThreadListPreferences());
        MainWindowViewModel viewModel = CreatePreferenceViewModel(records, preferenceStore);
        await viewModel.RefreshAsync();
        string[] before = viewModel.Threads.Select(row => row.Id).ToArray();

        viewModel.ToggleFavorite("b");

        Assert.Equal(before, viewModel.Threads.Select(row => row.Id));
        Assert.True(viewModel.Threads.Single(row => row.Id == "b").IsFavorite);
        Assert.Contains("b", preferenceStore.LastSaved!.FavoriteThreadIds);
    }

    [Fact]
    public async Task ToggleFavoritesOnly_ImmediatelyFiltersRowsAndKeepsMissingFavorite()
    {
        ThreadRecord[] records = [Record("favorite"), Record("other")];
        var preferenceStore = new MemoryThreadListPreferenceStore(new ThreadListPreferences(
            favoriteThreadIds: ["favorite", "missing"]));
        MainWindowViewModel viewModel = CreatePreferenceViewModel(records, preferenceStore);
        await viewModel.RefreshAsync();

        viewModel.ToggleFavoritesOnly();

        Assert.True(viewModel.ShowsFavoritesOnly);
        Assert.Equal("显示全部任务", viewModel.FavoritesFilterTooltip);
        Assert.Equal("favorite", Assert.Single(viewModel.Threads).Id);
        Assert.True(preferenceStore.LastSaved!.FavoriteThreadIds.SetEquals(["favorite", "missing"]));

        viewModel.ToggleFavoritesOnlyCommand.Execute(null);

        Assert.False(viewModel.ShowsFavoritesOnly);
        Assert.Equal("仅显示收藏", viewModel.FavoritesFilterTooltip);
        Assert.Equal(["favorite", "other"], viewModel.Threads.Select(row => row.Id));
    }

    [Fact]
    public async Task RefreshAsync_ArchivedFavoriteIsSanitizedBeforeNotificationObservation()
    {
        ThreadRecord active = Record("active");
        ThreadRecord archived = new("archived", "Archived", "archived", Now, 50, 0, true);
        var repository = new FavoriteOnlyThreadRepository([active], [archived]);
        var preferenceStore = new MemoryThreadListPreferenceStore(new ThreadListPreferences(
            favoriteThreadIds: ["archived"]));
        var observer = new RecordingCompletionObserver();
        var loader = new ThreadStatusLoader(
            repository,
            new HealthyTitleRepository(),
            new StatusRolloutParser(new Dictionary<string, ThreadStatus>
            {
                ["active"] = ThreadStatus.Running,
                ["archived"] = ThreadStatus.JustCompleted,
            }),
            new FixedTimeProvider(Now));
        var viewModel = new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState(),
            observer,
            preferenceStore,
            new FixedTimeProvider(Now));

        await viewModel.RefreshAsync(RefreshNotificationPolicy.Notify);

        ThreadSnapshot observed = Assert.Single(observer.LastSnapshots!, item => item.Id == "archived");
        Assert.True(observed.IsArchived);
        Assert.Equal(ThreadStatus.Idle, observed.Status);
        Assert.Null(observed.CompletionEventAt);
        Assert.Null(observed.ServiceIncident);
    }

    [Fact]
    public async Task ToggleFavorite_UnfavoritingArchivedTaskRemovesItImmediatelyInAllMode()
    {
        ThreadRecord active = Record("active");
        ThreadRecord archived = new("archived", "Archived", "archived", Now, 50, 0, true);
        var repository = new FavoriteOnlyThreadRepository([active], [archived]);
        var preferenceStore = new MemoryThreadListPreferenceStore(new ThreadListPreferences(
            favoriteThreadIds: ["archived"]));
        var loader = new ThreadStatusLoader(
            repository,
            new HealthyTitleRepository(),
            new StatusRolloutParser(new Dictionary<string, ThreadStatus>
            {
                ["active"] = ThreadStatus.Running,
                ["archived"] = ThreadStatus.Idle,
            }),
            new FixedTimeProvider(Now));
        var viewModel = new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState(),
            preferenceStore: preferenceStore,
            timeProvider: new FixedTimeProvider(Now));
        await viewModel.RefreshAsync();
        Assert.Equal(["active", "archived"], viewModel.Threads.Select(row => row.Id));

        viewModel.ToggleFavorite("archived");

        Assert.Equal("active", Assert.Single(viewModel.Threads).Id);
        Assert.Empty(preferenceStore.LastSaved!.FavoriteThreadIds);
    }

    [Fact]
    public async Task RefreshAsync_DoesNotReinsertArchivedTaskUnfavoritedDuringLoad()
    {
        ThreadRecord active = Record("active");
        ThreadRecord archived = new("archived", "Archived", "archived", Now, 50, 0, true);
        var repository = new BlockingFavoriteThreadRepository([active], [archived]);
        var preferenceStore = new MemoryThreadListPreferenceStore(new ThreadListPreferences(
            favoriteThreadIds: ["archived"]));
        var loader = new ThreadStatusLoader(
            repository,
            new HealthyTitleRepository(),
            new StatusRolloutParser(new Dictionary<string, ThreadStatus>
            {
                ["active"] = ThreadStatus.Running,
                ["archived"] = ThreadStatus.Idle,
            }),
            new FixedTimeProvider(Now));
        var viewModel = new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState(),
            preferenceStore: preferenceStore,
            timeProvider: new FixedTimeProvider(Now));

        Task refresh = viewModel.RefreshAsync();
        Assert.True(repository.FavoriteLoadStarted.Wait(TimeSpan.FromSeconds(5)));
        viewModel.ToggleFavorite("archived");
        repository.ContinueFavoriteLoad.Set();
        await refresh;

        Assert.Equal("active", Assert.Single(viewModel.Threads).Id);
        Assert.Empty(preferenceStore.LastSaved!.FavoriteThreadIds);
    }

    private static MainWindowViewModel CreateViewModel(
        MonitoringState monitoring,
        ThreadRepositoryStatus repositoryStatus,
        ICompletionNotificationObserver? observer = null)
    {
        var loader = new ThreadStatusLoader(
            new FakeThreadRepository(repositoryStatus),
            new HealthyTitleRepository(),
            new UnusedRolloutParser());
        var windowPin = new WindowPinState(new MemorySettingsStore());
        return new MainWindowViewModel(loader, windowPin, monitoring, observer);
    }

    private static MainWindowViewModel CreateCountViewModel(
        MutableThreadRepository repository,
        IReadOnlyDictionary<string, ThreadStatus> statuses)
    {
        var loader = new ThreadStatusLoader(
            repository,
            new HealthyTitleRepository(),
            new StatusRolloutParser(statuses),
            new FixedTimeProvider(Now));
        return new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState());
    }

    private static ThreadRecord Record(string id) =>
        new(id, id, id, Now, 0, 0);

    private static MainWindowViewModel CreatePreferenceViewModel(
        IReadOnlyList<ThreadRecord> records,
        IThreadListPreferenceStore preferenceStore,
        PreferenceThreadRepository? repository = null)
    {
        repository ??= new PreferenceThreadRepository(records);
        var loader = new ThreadStatusLoader(
            repository,
            new HealthyTitleRepository(),
            new StatusRolloutParser(records.ToDictionary(
                record => record.Id,
                _ => ThreadStatus.Running,
                StringComparer.Ordinal)),
            new FixedTimeProvider(Now));
        return new MainWindowViewModel(
            loader,
            new WindowPinState(new MemorySettingsStore()),
            new MonitoringState(),
            preferenceStore: preferenceStore,
            timeProvider: new FixedTimeProvider(Now));
    }

    private sealed class FakeThreadRepository(ThreadRepositoryStatus status)
        : IThreadRepository
    {
        public ThreadLoadResult LoadRecent(int limit = 8) => new(status, []);
    }

    private sealed class HealthyTitleRepository : ISessionIndexTitleRepository
    {
        public TitleLoadResult LoadLatestTitles() =>
            new(SessionIndexStatus.Healthy, new Dictionary<string, string>());
    }

    private sealed class UnusedRolloutParser : IRolloutTailParser
    {
        public RolloutLoadResult Parse(string filePath) =>
            throw new InvalidOperationException("No empty-source rollout should be parsed.");
    }

    private sealed class MemorySettingsStore : IAppSettingsStore
    {
        public AppSettings Load() => new();

        public bool Save(AppSettings settings) => true;
    }

    private sealed class MemoryDisplaySettingsStore(DisplaySettings settings)
        : IDisplaySettingsStore
    {
        public DisplaySettings Load() => settings;

        public bool Save(DisplaySettings updatedSettings) => true;
    }

    private sealed class ThrowingThreadRepository : IThreadRepository
    {
        public ThreadLoadResult LoadRecent(int limit = 8) =>
            throw new IOException("Database unavailable.");
    }

    private sealed class MutableThreadRepository(IReadOnlyList<ThreadRecord> records)
        : IThreadRepository
    {
        public bool ThrowOnLoad { get; set; }

        public ThreadRepositoryStatus Status { get; set; } = ThreadRepositoryStatus.Healthy;

        public ThreadLoadResult LoadRecent(int limit = 8)
        {
            if (ThrowOnLoad)
            {
                throw new IOException("Database unavailable.");
            }

            return new ThreadLoadResult(
                Status,
                Status is ThreadRepositoryStatus.Healthy ? records : []);
        }
    }

    private sealed class PreferenceThreadRepository(IReadOnlyList<ThreadRecord> records)
        : IThreadRepository
    {
        public int LastRecentLimit { get; private set; }

        public IReadOnlySet<string>? LastIncludedIds { get; private set; }

        public IReadOnlySet<string>? LastFavoriteIds { get; private set; }

        public ThreadLoadResult LoadRecent(int limit = 8)
        {
            LastRecentLimit = limit;
            return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, records);
        }

        public ThreadLoadResult LoadByIds(IReadOnlySet<string> threadIds)
        {
            LastIncludedIds = new HashSet<string>(threadIds, StringComparer.Ordinal);
            return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, records);
        }

        public ThreadLoadResult LoadByIdsIncludingArchived(IReadOnlySet<string> threadIds)
        {
            LastFavoriteIds = new HashSet<string>(threadIds, StringComparer.Ordinal);
            return new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                records.Where(record => threadIds.Contains(record.Id)).ToArray());
        }
    }

    private sealed class SupplementalFailureThreadRepository(ThreadRecord recent)
        : IThreadRepository
    {
        public ThreadLoadResult LoadRecent(int limit = 8) =>
            new(ThreadRepositoryStatus.Healthy, [recent]);

        public ThreadLoadResult LoadByIds(IReadOnlySet<string> threadIds) =>
            new(ThreadRepositoryStatus.Busy, []);
    }

    private sealed class FavoriteOnlyThreadRepository(
        IReadOnlyList<ThreadRecord> recent,
        IReadOnlyList<ThreadRecord> favorites) : IThreadRepository
    {
        public ThreadLoadResult LoadRecent(int limit = 8) =>
            new(ThreadRepositoryStatus.Healthy, recent);

        public ThreadLoadResult LoadByIdsIncludingArchived(IReadOnlySet<string> threadIds) =>
            new(
                ThreadRepositoryStatus.Healthy,
                favorites.Where(record => threadIds.Contains(record.Id)).ToArray());
    }

    private sealed class BlockingFavoriteThreadRepository(
        IReadOnlyList<ThreadRecord> recent,
        IReadOnlyList<ThreadRecord> favorites) : IThreadRepository
    {
        public ManualResetEventSlim FavoriteLoadStarted { get; } = new(false);

        public ManualResetEventSlim ContinueFavoriteLoad { get; } = new(false);

        public ThreadLoadResult LoadRecent(int limit = 8) =>
            new(ThreadRepositoryStatus.Healthy, recent);

        public ThreadLoadResult LoadByIdsIncludingArchived(IReadOnlySet<string> threadIds)
        {
            FavoriteLoadStarted.Set();
            if (!ContinueFavoriteLoad.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Test did not release the favorite load.");
            }

            return new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                favorites.Where(record => threadIds.Contains(record.Id)).ToArray());
        }
    }

    private sealed class ExpandableThreadRepository : IThreadRepository
    {
        public bool ThrowOnLoadRecent { get; set; }

        public IReadOnlySet<string>? LastRequestedParentIds { get; private set; }

        public int SubagentLoadCount { get; private set; }

        public ThreadLoadResult LoadRecent(int limit = 8)
        {
            if (ThrowOnLoadRecent)
            {
                throw new IOException("Unexpected fixture failure.");
            }

            return new(
                ThreadRepositoryStatus.Healthy,
                [new ThreadRecord("parent", "Parent", "parent", Now, 0, 1)]);
        }

        public SubagentLoadResult LoadDirectSubagents(IReadOnlySet<string> parentIds)
        {
            SubagentLoadCount++;
            LastRequestedParentIds = new HashSet<string>(parentIds, StringComparer.Ordinal);
            return new SubagentLoadResult(
                ThreadRepositoryStatus.Healthy,
                new Dictionary<string, IReadOnlyList<SubagentRecord>>(StringComparer.Ordinal)
                {
                    ["parent"] =
                    [
                        new SubagentRecord(
                            "child",
                            "parent",
                            "Child",
                            "child",
                            Now,
                            10,
                            "worker",
                            "reviewer",
                            "gpt-test",
                            "high"),
                    ],
                });
        }
    }

    private sealed class StatusRolloutParser(
        IReadOnlyDictionary<string, ThreadStatus> statuses) : IRolloutTailParser
    {
        public RolloutLoadResult Parse(string filePath)
        {
            ThreadStatus status = statuses[filePath];
            return new RolloutLoadResult(
                RolloutSourceStatus.Healthy,
                new RolloutObservation(status, Now, Now, null, null, null));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MemoryThreadListPreferenceStore(ThreadListPreferences initial)
        : IThreadListPreferenceStore
    {
        public ThreadListPreferences? LastSaved { get; private set; }

        public ThreadListPreferences Load() => initial.Clone();

        public bool Save(ThreadListPreferences preferences)
        {
            LastSaved = preferences.Clone();
            return true;
        }
    }

    private sealed class RecordingCompletionObserver : ICompletionNotificationObserver
    {
        public IReadOnlyList<ThreadSnapshot>? LastSnapshots { get; private set; }

        public RefreshNotificationPolicy? LastPolicy { get; private set; }

        public int ObservationCount { get; private set; }

        public void Observe(
            IReadOnlyList<ThreadSnapshot> snapshots,
            RefreshNotificationPolicy policy)
        {
            ObservationCount++;
            LastSnapshots = snapshots;
            LastPolicy = policy;
        }
    }
}
