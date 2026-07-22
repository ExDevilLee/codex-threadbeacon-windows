using ThreadBeacon.App.Localization;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.Localization;

public sealed class AppLanguageTextTests
{
    [Fact]
    public void Duration_PluralizesEnglishDays()
    {
        Assert.Equal("1 day", AppLanguageText.Duration(AppLanguage.English, TimeSpan.FromDays(1)));
        Assert.Equal("4 days", AppLanguageText.Duration(AppLanguage.English, TimeSpan.FromDays(4)));
    }

    [Fact]
    public void Status_LocalizesInterrupted()
    {
        Assert.Equal("已中断", AppLanguageText.Status(AppLanguage.SimplifiedChinese, ThreadStatus.Interrupted));
        Assert.Equal("Interrupted", AppLanguageText.Status(AppLanguage.English, ThreadStatus.Interrupted));
    }
}
