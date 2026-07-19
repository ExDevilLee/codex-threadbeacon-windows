using ThreadBeacon.App.Formatting;

namespace ThreadBeacon.App.Tests.Formatting;

public sealed class SubagentAliasFormatterTests
{
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
