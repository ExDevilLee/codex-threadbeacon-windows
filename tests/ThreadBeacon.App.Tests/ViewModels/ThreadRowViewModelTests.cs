using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class ThreadRowViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ZeroCountHidesSubagentPresentation()
    {
        var viewModel = new ThreadRowViewModel(Snapshot(subagentCount: 0), Now);

        Assert.Equal(0, viewModel.SubagentCount);
        Assert.False(viewModel.HasSubagents);
        Assert.Equal(string.Empty, viewModel.SubagentCountText);
        Assert.Equal(string.Empty, viewModel.SubagentAccessibilityLabel);
    }

    [Fact]
    public void Constructor_PositiveCountExposesExactSubagentPresentation()
    {
        var viewModel = new ThreadRowViewModel(Snapshot(subagentCount: 3), Now);

        Assert.Equal(3, viewModel.SubagentCount);
        Assert.True(viewModel.HasSubagents);
        Assert.Equal("3", viewModel.SubagentCountText);
        Assert.Equal("3 个 Subagent", viewModel.SubagentAccessibilityLabel);
    }

    [Fact]
    public void Constructor_NegativeCountNormalizesToHiddenState()
    {
        var viewModel = new ThreadRowViewModel(Snapshot(subagentCount: -1), Now);

        Assert.Equal(0, viewModel.SubagentCount);
        Assert.False(viewModel.HasSubagents);
    }

    [Fact]
    public void ExpansionState_UpdatesTogglePresentationAndInvokesParentCallback()
    {
        string? toggledId = null;
        var viewModel = new ThreadRowViewModel(
            Snapshot(subagentCount: 3),
            Now,
            id =>
            {
                toggledId = id;
                return Task.CompletedTask;
            });

        Assert.False(viewModel.IsSubagentExpanded);
        Assert.Equal("展开 3 个 Subagent", viewModel.SubagentToggleAccessibilityLabel);

        viewModel.SetSubagentExpanded(true, isLoading: true);
        viewModel.ToggleSubagentsCommand.Execute(null);

        Assert.True(viewModel.IsSubagentExpanded);
        Assert.True(viewModel.IsSubagentLoading);
        Assert.Equal("收起 3 个 Subagent", viewModel.SubagentToggleAccessibilityLabel);
        Assert.Equal("thread-1", toggledId);
    }

    [Fact]
    public void ExpandedEmptyRegion_ReportsLoadingFailureAndEmptyStates()
    {
        var viewModel = new ThreadRowViewModel(Snapshot(subagentCount: 1), Now);

        viewModel.SetSubagentExpanded(true, isLoading: true);
        Assert.True(viewModel.ShowSubagentPlaceholder);
        Assert.Equal("正在读取 Subagent", viewModel.SubagentPlaceholderText);

        viewModel.Update(
            Snapshot(
                subagentCount: 1,
                subagentSourceStatus: ThreadRepositoryStatus.Busy),
            Now);
        viewModel.SetSubagentExpanded(true, isLoading: false);
        Assert.Equal("Subagent 读取失败", viewModel.SubagentPlaceholderText);

        viewModel.Update(Snapshot(subagentCount: 1), Now);
        viewModel.SetSubagentExpanded(true, isLoading: false);
        Assert.Equal("暂无可读取的 Subagent", viewModel.SubagentPlaceholderText);
    }

    [Fact]
    public void Constructor_RetryIncidentShowsServiceDetail()
    {
        var viewModel = new ThreadRowViewModel(
            Snapshot(
                subagentCount: 0,
                serviceIncident: new ServiceIncident(
                    "turn-retry",
                    ServiceIncidentPhase.Retrying,
                    429,
                    2,
                    5,
                    Now.AddSeconds(-10))),
            Now);

        Assert.Equal("服务异常", viewModel.StatusLabel);
        Assert.Equal("HTTP 429 · 重试 2/5", viewModel.IncidentDetailText);
        Assert.True(viewModel.HasIncidentDetail);
    }

    [Fact]
    public void Constructor_FailedIncidentOmitsUnavailableDetail()
    {
        var viewModel = new ThreadRowViewModel(
            Snapshot(
                subagentCount: 0,
                serviceIncident: new ServiceIncident(
                    "turn-failed",
                    ServiceIncidentPhase.Failed,
                    null,
                    null,
                    null,
                    Now.AddSeconds(-10))),
            Now);

        Assert.Equal("服务失败", viewModel.StatusLabel);
        Assert.Equal(string.Empty, viewModel.IncidentDetailText);
        Assert.False(viewModel.HasIncidentDetail);
    }

    [Fact]
    public void Constructor_NormalTaskHasNoIncidentPresentation()
    {
        var viewModel = new ThreadRowViewModel(Snapshot(subagentCount: 0), Now);

        Assert.Equal("运行中", viewModel.StatusLabel);
        Assert.Equal(string.Empty, viewModel.IncidentDetailText);
        Assert.False(viewModel.HasIncidentDetail);
    }

    private static ThreadSnapshot Snapshot(
        int subagentCount,
        ThreadRepositoryStatus subagentSourceStatus = ThreadRepositoryStatus.Healthy,
        ServiceIncident? serviceIncident = null) =>
        new(
            "thread-1",
            "Task",
            ThreadStatus.Running,
            Now.AddMinutes(-1),
            Now,
            Now,
            Now.AddMinutes(-1),
            null,
            null,
            subagentCount,
            RolloutSourceStatus.Healthy,
            subagentSourceStatus: subagentSourceStatus,
            serviceIncident: serviceIncident);
}
