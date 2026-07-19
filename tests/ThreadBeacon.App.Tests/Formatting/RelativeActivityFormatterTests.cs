using ThreadBeacon.App.Formatting;

namespace ThreadBeacon.App.Tests.Formatting;

public sealed class RelativeActivityFormatterTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(20, "刚刚")]
    [InlineData(120, "2 分钟前")]
    [InlineData(7_200, "2 小时前")]
    [InlineData(172_800, "2 天前")]
    public void Format_UsesCompactMacCompatibleBuckets(int seconds, string expected)
    {
        Assert.Equal(expected, RelativeActivityFormatter.Format(Now.AddSeconds(-seconds), Now));
    }
}
