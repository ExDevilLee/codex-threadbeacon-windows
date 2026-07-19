using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class SubagentRowViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_MapsCompactRowAndDetailFields()
    {
        var tokenUsage = new TokenUsageSnapshot(
            3_200,
            new TokenUsage(2_000, 800, 300, 100, 3_200),
            new TokenUsage(300, 100, 80, 20, 500),
            Now.AddMinutes(-2));
        var snapshot = new SubagentSnapshot(
            "child",
            "Review task",
            ThreadStatus.Running,
            Now.AddMinutes(-5),
            Now.AddMinutes(-3),
            Now.AddMinutes(-2),
            tokenUsage,
            " worker ",
            "reviewer",
            "gpt-test",
            "high",
            RolloutSourceStatus.Healthy);

        var row = new SubagentRowViewModel(snapshot, Now);

        Assert.Equal("worker", row.Alias);
        Assert.True(row.HasAlias);
        Assert.Equal("Review task", row.Title);
        Assert.Equal("运行中", row.StatusLabel);
        Assert.Equal("3.2K", row.TokenText);
        Assert.Equal("2 分钟前", row.ActivityText);
        Assert.Equal("reviewer", row.Details.Rows.Single(item => item.Label == "角色").Value);
        Assert.Equal("+500", row.Details.Rows.Single(item => item.Label == "当前 turn").Value);
    }
}
