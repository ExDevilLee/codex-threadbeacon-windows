using System.Globalization;

namespace ThreadBeacon.App.Localization;

public static class AppLanguageResolver
{
    public static AppLanguage Parse(string? rawValue) => rawValue switch
    {
        "zh-Hans" => AppLanguage.SimplifiedChinese,
        "en" => AppLanguage.English,
        "system" => AppLanguage.System,
        _ => AppLanguage.System,
    };

    public static string ToStorageValue(AppLanguage language) => language switch
    {
        AppLanguage.SimplifiedChinese => "zh-Hans",
        AppLanguage.English => "en",
        _ => "system",
    };

    public static AppLanguage ResolveSystem(CultureInfo? culture = null) =>
        ResolveSystem((culture ?? CultureInfo.CurrentUICulture).Name);

    public static AppLanguage ResolveSystem(string? cultureName) =>
        !string.IsNullOrWhiteSpace(cultureName)
            && cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.SimplifiedChinese
            : AppLanguage.English;

    public static AppLanguage Resolve(AppLanguage preference, CultureInfo? culture = null) =>
        preference is AppLanguage.System ? ResolveSystem(culture) : preference;
}
