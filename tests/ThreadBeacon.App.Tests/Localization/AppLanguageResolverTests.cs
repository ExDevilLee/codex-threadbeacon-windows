using ThreadBeacon.App.Localization;

namespace ThreadBeacon.App.Tests.Localization;

public sealed class AppLanguageResolverTests
{
    [Theory]
    [InlineData("system", AppLanguage.System)]
    [InlineData("zh-Hans", AppLanguage.SimplifiedChinese)]
    [InlineData("en", AppLanguage.English)]
    public void Parse_PreservesSupportedSemanticValues(string raw, AppLanguage expected)
    {
        Assert.Equal(expected, AppLanguageResolver.Parse(raw));
    }

    [Fact]
    public void Parse_InvalidValueFallsBackToSystem()
    {
        Assert.Equal(AppLanguage.System, AppLanguageResolver.Parse("简体中文"));
    }

    [Theory]
    [InlineData("zh-CN", AppLanguage.SimplifiedChinese)]
    [InlineData("zh-TW", AppLanguage.SimplifiedChinese)]
    [InlineData("en-US", AppLanguage.English)]
    [InlineData("ja-JP", AppLanguage.English)]
    public void ResolveSystem_UsesChineseForChineseLocalesAndEnglishOtherwise(
        string locale,
        AppLanguage expected)
    {
        Assert.Equal(expected, AppLanguageResolver.ResolveSystem(locale));
    }
}
