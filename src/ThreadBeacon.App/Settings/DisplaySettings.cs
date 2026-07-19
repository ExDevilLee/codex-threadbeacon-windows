using System.Text.Json.Serialization;

namespace ThreadBeacon.App.Settings;

public sealed record DisplaySettings
{
    private static readonly IReadOnlyList<int> RefreshIntervals =
        Array.AsReadOnly([1, 2, 5, 10]);
    private static readonly IReadOnlyList<int> MaximumTaskCounts =
        Array.AsReadOnly([4, 8, 12, 20]);

    public const int DefaultRefreshIntervalSeconds = 2;
    public const int DefaultMaximumTaskCount = 8;

    [JsonConstructor]
    public DisplaySettings(
        int refreshIntervalSeconds = DefaultRefreshIntervalSeconds,
        int maximumTaskCount = DefaultMaximumTaskCount,
        int version = 1)
    {
        RefreshIntervalSeconds = RefreshIntervals.Contains(refreshIntervalSeconds)
            ? refreshIntervalSeconds
            : DefaultRefreshIntervalSeconds;
        MaximumTaskCount = MaximumTaskCounts.Contains(maximumTaskCount)
            ? maximumTaskCount
            : DefaultMaximumTaskCount;
        Version = version;
    }

    public static IReadOnlyList<int> SupportedRefreshIntervalSeconds => RefreshIntervals;

    public static IReadOnlyList<int> SupportedMaximumTaskCounts => MaximumTaskCounts;

    public int Version { get; }

    public int RefreshIntervalSeconds { get; }

    public int MaximumTaskCount { get; }
}
