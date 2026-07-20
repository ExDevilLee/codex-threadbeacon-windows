using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Tests.Models;

public sealed class SemanticVersionTests
{
    [Theory]
    [InlineData("v1.2.3", 1, 2, 3, null)]
    [InlineData("1.2.3-beta.2+build.7", 1, 2, 3, "beta.2")]
    public void TryParse_AcceptsReleaseTags(
        string value,
        int major,
        int minor,
        int patch,
        string? prerelease)
    {
        Assert.True(SemanticVersion.TryParse(value, out SemanticVersion version));
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
        Assert.Equal(prerelease, version.Prerelease);
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("release-1.2.3")]
    [InlineData("1.2.-1")]
    public void TryParse_RejectsInvalidVersions(string value) =>
        Assert.False(SemanticVersion.TryParse(value, out _));

    [Fact]
    public void CompareTo_OrdersPrereleasesBeforeStableAndNumericIdentifiersNumerically()
    {
        SemanticVersion.TryParse("1.2.3-beta.2", out SemanticVersion beta2);
        SemanticVersion.TryParse("1.2.3-beta.10", out SemanticVersion beta10);
        SemanticVersion.TryParse("1.2.3", out SemanticVersion stable);

        Assert.True(beta2 < beta10);
        Assert.True(beta10 < stable);
    }
}
