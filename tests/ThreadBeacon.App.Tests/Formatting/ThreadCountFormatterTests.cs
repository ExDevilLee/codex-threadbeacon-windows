using ThreadBeacon.App.Formatting;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.Formatting;

public sealed class ThreadCountFormatterTests
{
    [Fact]
    public void Format_ShowsRunningAndVisibleCounts()
    {
        ThreadCountLabel result = ThreadCountFormatter.Format(
            [
                ThreadStatus.Running,
                ThreadStatus.Running,
                ThreadStatus.Running,
                ThreadStatus.Idle,
                ThreadStatus.JustCompleted,
                ThreadStatus.Unknown,
                ThreadStatus.NeedsAction,
                ThreadStatus.Error,
            ]);

        Assert.Equal("3/8", result.DisplayText);
        Assert.Equal("3 个任务正在运行，共显示 8 个任务", result.AccessibilityLabel);
    }

    [Fact]
    public void Format_EmptyListShowsZeroCounts()
    {
        ThreadCountLabel result = ThreadCountFormatter.Format([]);

        Assert.Equal("0/0", result.DisplayText);
        Assert.Equal("0 个任务正在运行，共显示 0 个任务", result.AccessibilityLabel);
    }

    [Fact]
    public void Format_NullStatusesThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ThreadCountFormatter.Format(null!));
    }
}
