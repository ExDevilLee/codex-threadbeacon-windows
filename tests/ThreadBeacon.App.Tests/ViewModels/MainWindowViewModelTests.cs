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

    private sealed class ThrowingThreadRepository : IThreadRepository
    {
        public ThreadLoadResult LoadRecent(int limit = 8) =>
            throw new IOException("Database unavailable.");
    }

    private sealed class MutableThreadRepository(IReadOnlyList<ThreadRecord> records)
        : IThreadRepository
    {
        public bool ThrowOnLoad { get; set; }

        public ThreadLoadResult LoadRecent(int limit = 8)
        {
            if (ThrowOnLoad)
            {
                throw new IOException("Database unavailable.");
            }

            return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, records);
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

    private sealed class RecordingCompletionObserver : ICompletionNotificationObserver
    {
        public IReadOnlyList<ThreadSnapshot>? LastSnapshots { get; private set; }

        public RefreshNotificationPolicy? LastPolicy { get; private set; }

        public void Observe(
            IReadOnlyList<ThreadSnapshot> snapshots,
            RefreshNotificationPolicy policy)
        {
            LastSnapshots = snapshots;
            LastPolicy = policy;
        }
    }
}
