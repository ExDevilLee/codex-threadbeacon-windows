namespace ThreadBeacon.App.Theme;

public enum AppTheme
{
    System,
    Light,
    Dark,
}

public static class AppThemeResolver
{
    public static AppTheme Parse(string? rawValue) => rawValue switch
    {
        "light" => AppTheme.Light,
        "dark" => AppTheme.Dark,
        _ => AppTheme.System,
    };

    public static string ToStorageValue(AppTheme theme) => theme switch
    {
        AppTheme.Light => "light",
        AppTheme.Dark => "dark",
        _ => "system",
    };
}
