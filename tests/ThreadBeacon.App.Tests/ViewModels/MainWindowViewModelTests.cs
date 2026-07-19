using ThreadBeacon.App.Settings;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;
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

    private static MainWindowViewModel CreateViewModel(
        MonitoringState monitoring,
        ThreadRepositoryStatus repositoryStatus)
    {
        var loader = new ThreadStatusLoader(
            new FakeThreadRepository(repositoryStatus),
            new HealthyTitleRepository(),
            new UnusedRolloutParser());
        var windowPin = new WindowPinState(new MemorySettingsStore());
        return new MainWindowViewModel(loader, windowPin, monitoring);
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
}
