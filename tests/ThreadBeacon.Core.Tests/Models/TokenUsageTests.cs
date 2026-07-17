using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Tests.Models;

public sealed class TokenUsageTests
{
    [Fact]
    public void Subtract_ReturnsCumulativeDelta()
    {
        var baseline = new TokenUsage(900, 400, 100, 30, 1_000);
        var current = new TokenUsage(1_350, 650, 150, 40, 1_500);

        TokenUsage? result = current.Subtract(baseline);

        Assert.Equal(new TokenUsage(450, 250, 50, 10, 500), result);
        Assert.Equal(700, current.UncachedInputTokens);
        Assert.Equal(650d / 1_350d, current.CacheRatio);
    }

    [Fact]
    public void Subtract_RejectsBackwardCounters()
    {
        var baseline = new TokenUsage(900, 400, 100, 30, 1_000);
        var current = new TokenUsage(800, 350, 90, 20, 890);

        Assert.Null(current.Subtract(baseline));
    }
}
