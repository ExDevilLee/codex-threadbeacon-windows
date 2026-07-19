using ThreadBeacon.App.Localization;

namespace ThreadBeacon.App.Tests.Localization;

public sealed class AppLanguageStateTests
{
    [Fact]
    public void SetPreference_NotifiesOnlyWhenPreferenceChanges()
    {
        var state = new AppLanguageState(AppLanguage.System, "en-US");
        int changeCount = 0;
        state.Changed += (_, _) => changeCount++;

        state.SetPreference(AppLanguage.English);
        state.SetPreference(AppLanguage.English);

        Assert.Equal(1, changeCount);
        Assert.Equal(AppLanguage.English, state.EffectiveLanguage);
    }

    [Fact]
    public void SystemPreference_ResolvesToSimplifiedChineseForChineseLocale()
    {
        var state = new AppLanguageState(AppLanguage.System, "zh-CN");

        Assert.Equal(AppLanguage.SimplifiedChinese, state.EffectiveLanguage);
    }
}
