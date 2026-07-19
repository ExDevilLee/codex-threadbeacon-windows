namespace ThreadBeacon.App.Formatting;

public static class RelativeActivityFormatter
{
    public static string Format(DateTimeOffset activityAt, DateTimeOffset now)
    {
        TimeSpan elapsed = now - activityAt;
        elapsed = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        return elapsed.TotalSeconds switch
        {
            < 60 => "刚刚",
            < 3_600 => $"{Math.Floor(elapsed.TotalMinutes)} 分钟前",
            < 86_400 => $"{Math.Floor(elapsed.TotalHours)} 小时前",
            _ => $"{Math.Floor(elapsed.TotalDays)} 天前",
        };
    }
}
