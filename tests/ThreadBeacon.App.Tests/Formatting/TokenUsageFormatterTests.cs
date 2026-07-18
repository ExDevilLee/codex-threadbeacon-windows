using ThreadBeacon.App.Formatting;

namespace ThreadBeacon.App.Tests.Formatting;

public sealed class TokenUsageFormatterTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(999, "999")]
    [InlineData(1_000, "1K")]
    [InlineData(1_260, "1.3K")]
    [InlineData(1_000_000, "1M")]
    [InlineData(1_000_000_000, "1B")]
    public void FormatCount_UsesCompactText(long value, string expected) =>
        Assert.Equal(expected, TokenUsageFormatter.FormatCount(value));

    [Fact]
    public void FormatCount_WhenMissing_ReturnsDash() =>
        Assert.Equal("—", TokenUsageFormatter.FormatCount(null));

    [Fact]
    public void FormatCount_WhenNegative_ReturnsDash() =>
        Assert.Equal("—", TokenUsageFormatter.FormatCount(-1L));

    [Fact]
    public void FormatCurrentTurn_PrefixesKnownValue() =>
        Assert.Equal("+1.5K", TokenUsageFormatter.FormatCurrentTurn(1_500));

    [Fact]
    public void FormatCurrentTurn_WhenUnavailable_ReturnsDash() =>
        Assert.Equal("—", TokenUsageFormatter.FormatCurrentTurn(null));

    [Fact]
    public void FormatPercent_UsesWholePercentage() =>
        Assert.Equal("40%", TokenUsageFormatter.FormatPercent(0.4));

    [Fact]
    public void FormatPercent_WhenUnavailable_ReturnsDash() =>
        Assert.Equal("—", TokenUsageFormatter.FormatPercent(null));

    [Fact]
    public void FormatTime_UsesLocalClockTime()
    {
        var timestamp = new DateTimeOffset(2026, 7, 18, 12, 34, 56, TimeSpan.Zero);

        Assert.Equal(timestamp.ToLocalTime().ToString("HH:mm:ss"), TokenUsageFormatter.FormatTime(timestamp));
    }
}
