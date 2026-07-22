using ThreadBeacon.App.Formatting;

namespace ThreadBeacon.App.Tests.Formatting;

public sealed class SubagentAliasFormatterTests
{
    [Fact]
    public void Format_PrefersHumanizedSemanticTaskName()
    {
        Assert.Equal(
            "Fix external sync",
            SubagentAliasFormatter.Format(
                "/root/fix_external_sync",
                "Lagrange",
                "Review workspace"));
    }

    [Theory]
    [InlineData(@"C:\root\audit-release", "Audit release")]
    [InlineData("/root/Review workspace", null)]
    [InlineData("/root/", "Root")]
    public void Format_NormalizesPathComponentsAndHidesTitleDuplicates(
        string agentPath,
        string? expected)
    {
        Assert.Equal(
            expected,
            SubagentAliasFormatter.Format(agentPath, "nickname", "Review workspace"));
    }

    [Fact]
    public void Format_MissingPathFallsBackToNickname()
    {
        Assert.Equal(
            "explorer",
            SubagentAliasFormatter.Format(null, " explorer ", "Review task"));
    }

    [Theory]
    [InlineData(" worker ", "Review task", "worker")]
    [InlineData("Review task", "Review task", null)]
    [InlineData("   ", "Review task", null)]
    [InlineData(null, "Review task", null)]
    public void Format_HidesEmptyOrDuplicateNickname(
        string? nickname,
        string title,
        string? expected)
    {
        Assert.Equal(expected, SubagentAliasFormatter.Format(nickname, title));
    }
}
