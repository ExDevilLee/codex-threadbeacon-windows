using ThreadBeacon.App.ViewModels;
using ThreadBeacon.App.Localization;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class SubagentRowViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ThreadStatus.Error)]
    [InlineData(ThreadStatus.NeedsAction)]
    [InlineData(ThreadStatus.Warning)]
    [InlineData(ThreadStatus.Running)]
    [InlineData(ThreadStatus.JustCompleted)]
    [InlineData(ThreadStatus.Idle)]
    [InlineData(ThreadStatus.Unknown)]
    public void Constructor_ExposesSameStatusGlyphContractAsMainRows(ThreadStatus status)
    {
        var snapshot = new SubagentSnapshot(
            "child",
            "Task",
            status,
            Now,
            Now,
            Now,
            null,
            null,
            null,
            null,
            null,
            RolloutSourceStatus.Healthy);

        var subagent = new SubagentRowViewModel(snapshot, Now, AppLanguage.English);
        var main = new ThreadRowViewModel(
            new ThreadSnapshot(
                "main", "Task", status, Now, Now, Now, Now, null, null, 0,
                RolloutSourceStatus.Healthy),
            Now,
            language: AppLanguage.English);

        Assert.Equal(main.StatusGlyph, subagent.StatusGlyph);
    }

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

    [Fact]
    public void Constructor_UsesEnglishCompactAndDetailLabels()
    {
        var snapshot = new SubagentSnapshot(
            "child",
            "Review task",
            ThreadStatus.Running,
            Now.AddMinutes(-5),
            Now.AddMinutes(-3),
            Now.AddMinutes(-2),
            null,
            "worker",
            "reviewer",
            "gpt-test",
            "high",
            RolloutSourceStatus.Healthy);

        var row = new SubagentRowViewModel(snapshot, Now, AppLanguage.English);

        Assert.Equal("Running", row.StatusLabel);
        Assert.Equal("2 min ago", row.ActivityText);
        Assert.Equal("reviewer", row.Details.Rows.Single(item => item.Label == "Role").Value);
        Assert.Contains(row.Details.Rows, item => item.Label == "Latest activity");
    }

    [Fact]
    public void Constructor_PrefersSemanticAgentPathAlias()
    {
        var snapshot = new SubagentSnapshot(
            "child",
            "Review workspace",
            ThreadStatus.Running,
            Now,
            Now,
            Now,
            null,
            "Lagrange",
            "reviewer",
            "gpt-test",
            "high",
            RolloutSourceStatus.Healthy,
            "/root/fix_external_sync");

        var row = new SubagentRowViewModel(snapshot, Now, AppLanguage.English);

        Assert.Equal("Fix external sync", row.Alias);
    }
}
