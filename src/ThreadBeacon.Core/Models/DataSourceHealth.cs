namespace ThreadBeacon.Core.Models;

public enum DataSourceHealthLevel
{
    Healthy,
    Degraded,
    Unavailable,
    NotUsed,
}

public enum OverallDataSourceHealth
{
    Healthy,
    Degraded,
    Unavailable,
}

public sealed record DataSourceHealthStatus(
    DataSourceHealthLevel Level,
    string DisplayText,
    string? DetailText)
{
    public static DataSourceHealthStatus Healthy { get; } =
        new(DataSourceHealthLevel.Healthy, "正常", null);

    public static DataSourceHealthStatus NotUsed { get; } =
        new(DataSourceHealthLevel.NotUsed, "未使用", null);

    public static DataSourceHealthStatus Degraded(string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        return new(DataSourceHealthLevel.Degraded, "部分降级", detail);
    }

    public static DataSourceHealthStatus Unavailable(string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        return new(DataSourceHealthLevel.Unavailable, "不可用", detail);
    }
}

public sealed record DataSourceHealthReport
{
    public DataSourceHealthReport(
        DataSourceHealthStatus taskDatabase,
        DataSourceHealthStatus renameIndex,
        DataSourceHealthStatus rollout,
        DataSourceHealthStatus serviceLogs,
        int rolloutSuccessCount,
        int rolloutFailureCount,
        DateTimeOffset? lastSuccessfulRefreshAt)
    {
        TaskDatabase = taskDatabase ?? throw new ArgumentNullException(nameof(taskDatabase));
        RenameIndex = renameIndex ?? throw new ArgumentNullException(nameof(renameIndex));
        Rollout = rollout ?? throw new ArgumentNullException(nameof(rollout));
        ServiceLogs = serviceLogs ?? throw new ArgumentNullException(nameof(serviceLogs));
        RolloutSuccessCount = Math.Max(0, rolloutSuccessCount);
        RolloutFailureCount = Math.Max(0, rolloutFailureCount);
        LastSuccessfulRefreshAt = lastSuccessfulRefreshAt;
    }

    public DataSourceHealthStatus TaskDatabase { get; }
    public DataSourceHealthStatus RenameIndex { get; }
    public DataSourceHealthStatus Rollout { get; }
    public DataSourceHealthStatus ServiceLogs { get; }
    public int RolloutSuccessCount { get; }
    public int RolloutFailureCount { get; }
    public DateTimeOffset? LastSuccessfulRefreshAt { get; }

    public OverallDataSourceHealth OverallStatus
    {
        get
        {
            if (TaskDatabase.Level is DataSourceHealthLevel.Unavailable)
            {
                return OverallDataSourceHealth.Unavailable;
            }

            DataSourceHealthStatus[] sources =
                [TaskDatabase, RenameIndex, Rollout, ServiceLogs];
            return sources.Any(source => source.Level is
                DataSourceHealthLevel.Degraded or DataSourceHealthLevel.Unavailable)
                ? OverallDataSourceHealth.Degraded
                : OverallDataSourceHealth.Healthy;
        }
    }

    public string Summary => OverallStatus switch
    {
        OverallDataSourceHealth.Healthy => "数据源正常",
        OverallDataSourceHealth.Degraded => "部分数据源降级",
        OverallDataSourceHealth.Unavailable => "任务数据不可用",
        _ => throw new InvalidOperationException("Unknown data source health state."),
    };

    public DataSourceHealthReport WithLastSuccessfulRefresh(DateTimeOffset value) =>
        new(
            TaskDatabase,
            RenameIndex,
            Rollout,
            ServiceLogs,
            RolloutSuccessCount,
            RolloutFailureCount,
            value);
}
