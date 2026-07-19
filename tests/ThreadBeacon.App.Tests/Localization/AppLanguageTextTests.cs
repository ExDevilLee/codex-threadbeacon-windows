using ThreadBeacon.App.Localization;

namespace ThreadBeacon.App.Tests.Localization;

public sealed class AppLanguageTextTests
{
    [Fact]
    public void Duration_PluralizesEnglishDays()
    {
        Assert.Equal("1 day", AppLanguageText.Duration(AppLanguage.English, TimeSpan.FromDays(1)));
        Assert.Equal("4 days", AppLanguageText.Duration(AppLanguage.English, TimeSpan.FromDays(4)));
    }
}
