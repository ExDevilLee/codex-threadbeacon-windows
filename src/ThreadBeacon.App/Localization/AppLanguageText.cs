namespace ThreadBeacon.App.Localization;

public static class AppLanguageText
{
    public static string RefreshSeconds(AppLanguage language, int value) => language switch
    {
        AppLanguage.SimplifiedChinese => $"{value} 秒",
        _ => $"{value} sec",
    };

    public static string TaskCount(AppLanguage language, int value) => language switch
    {
        AppLanguage.SimplifiedChinese => $"{value} 个",
        _ => $"{value} tasks",
    };

    public static string LanguageName(AppLanguage language) => language switch
    {
        AppLanguage.SimplifiedChinese => "简体中文",
        AppLanguage.English => "English",
        _ => "跟随系统 / System",
    };
}
