namespace ThreadBeacon.App.Formatting;

using ThreadBeacon.App.Localization;

public static class RelativeActivityFormatter
{
    public static string Format(
        DateTimeOffset activityAt,
        DateTimeOffset now,
        AppLanguage language = AppLanguage.SimplifiedChinese)
    {
        TimeSpan elapsed = now - activityAt;
        elapsed = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        return language is AppLanguage.SimplifiedChinese
            ? elapsed.TotalSeconds switch
        {
            < 60 => "刚刚",
            < 3_600 => $"{Math.Floor(elapsed.TotalMinutes)} 分钟前",
            < 86_400 => $"{Math.Floor(elapsed.TotalHours)} 小时前",
            _ => $"{Math.Floor(elapsed.TotalDays)} 天前",
        }
            : elapsed.TotalSeconds switch
            {
                < 60 => "Just now",
                < 3_600 => $"{Math.Floor(elapsed.TotalMinutes)} min ago",
                < 86_400 => $"{Math.Floor(elapsed.TotalHours)} hr ago",
                _ => $"{Math.Floor(elapsed.TotalDays)} days ago",
            };
    }
}
