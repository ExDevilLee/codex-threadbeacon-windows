using System.Text.Json.Serialization;
using ThreadBeacon.App.Localization;
using ThreadBeacon.App.Theme;

namespace ThreadBeacon.App.Settings;

public sealed record DisplaySettings
{
    private static readonly IReadOnlyList<int> RefreshIntervals =
        Array.AsReadOnly([1, 2, 5, 10]);
    private static readonly IReadOnlyList<int> MaximumTaskCounts =
        Array.AsReadOnly([4, 8, 12, 20]);
    private static readonly IReadOnlyList<int> JustCompletedRetentionMinutesOptions =
        Array.AsReadOnly([1, 2, 3, 4, 5]);

    public const int DefaultRefreshIntervalSeconds = 2;
    public const int DefaultMaximumTaskCount = 8;
    public const int DefaultJustCompletedRetentionMinutes = 1;

    [JsonConstructor]
    public DisplaySettings(
        int refreshIntervalSeconds = DefaultRefreshIntervalSeconds,
        int maximumTaskCount = DefaultMaximumTaskCount,
        int version = 1,
        AppLanguage language = AppLanguage.System,
        AppTheme theme = AppTheme.System,
        bool useColorBlindSafeStatusIndicators = false,
        int justCompletedRetentionMinutes = DefaultJustCompletedRetentionMinutes)
    {
        RefreshIntervalSeconds = RefreshIntervals.Contains(refreshIntervalSeconds)
            ? refreshIntervalSeconds
            : DefaultRefreshIntervalSeconds;
        MaximumTaskCount = MaximumTaskCounts.Contains(maximumTaskCount)
            ? maximumTaskCount
            : DefaultMaximumTaskCount;
        Version = version;
        Language = language;
        Theme = theme;
        UseColorBlindSafeStatusIndicators = useColorBlindSafeStatusIndicators;
        JustCompletedRetentionMinutes = JustCompletedRetentionMinutesOptions.Contains(
            justCompletedRetentionMinutes)
                ? justCompletedRetentionMinutes
                : DefaultJustCompletedRetentionMinutes;
    }

    public static IReadOnlyList<int> SupportedRefreshIntervalSeconds => RefreshIntervals;

    public static IReadOnlyList<int> SupportedMaximumTaskCounts => MaximumTaskCounts;

    public static IReadOnlyList<int> SupportedJustCompletedRetentionMinutes =>
        JustCompletedRetentionMinutesOptions;

    public int Version { get; }

    public int RefreshIntervalSeconds { get; }

    public int MaximumTaskCount { get; }

    [JsonConverter(typeof(AppLanguageJsonConverter))]
    public AppLanguage Language { get; }

    [JsonConverter(typeof(AppThemeJsonConverter))]
    public AppTheme Theme { get; }

    public bool UseColorBlindSafeStatusIndicators { get; }

    public int JustCompletedRetentionMinutes { get; }
}
