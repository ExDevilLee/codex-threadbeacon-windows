using ThreadBeacon.App.Theme;

namespace ThreadBeacon.App.Tests.Theme;

public sealed class WindowsSystemThemeDetectorTests
{
    [Theory]
    [InlineData(1, AppTheme.Light)]
    [InlineData(0, AppTheme.Dark)]
    public void ResolveRegistryValue_MapsWindowsAppTheme(int value, AppTheme expected)
    {
        Assert.Equal(expected, WindowsSystemThemeDetector.ResolveRegistryValue(value));
    }

    [Fact]
    public void ResolveRegistryValue_UsesLightFallbackForUnknownValue()
    {
        Assert.Equal(AppTheme.Light, WindowsSystemThemeDetector.ResolveRegistryValue(null));
        Assert.Equal(AppTheme.Light, WindowsSystemThemeDetector.ResolveRegistryValue(2));
    }
}
