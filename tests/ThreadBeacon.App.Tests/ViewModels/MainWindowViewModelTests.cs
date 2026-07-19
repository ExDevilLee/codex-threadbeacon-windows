using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Sounds;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
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
