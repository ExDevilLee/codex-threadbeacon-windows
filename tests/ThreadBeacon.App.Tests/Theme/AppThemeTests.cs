using ThreadBeacon.App.Theme;

namespace ThreadBeacon.App.Tests.Theme;

public sealed class AppThemeTests
{
    [Fact]
    public void Options_KeepMacCompatibleOrderAndDefault()
    {
        Assert.Equal([AppTheme.System, AppTheme.Light, AppTheme.Dark], Enum.GetValues<AppTheme>());
        Assert.Equal(AppTheme.System, AppThemeResolver.Parse(null));
        Assert.Equal(AppTheme.System, AppThemeResolver.Parse("unsupported"));
    }

    [Theory]
    [InlineData(AppTheme.System, "system")]
    [InlineData(AppTheme.Light, "light")]
    [InlineData(AppTheme.Dark, "dark")]
    public void StorageValues_AreStable(AppTheme theme, string expected)
    {
        Assert.Equal(expected, AppThemeResolver.ToStorageValue(theme));
        Assert.Equal(theme, AppThemeResolver.Parse(expected));
    }
}
