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

    private static ThreadSnapshot Snapshot(int subagentCount) =>
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
            RolloutSourceStatus.Healthy);
}
