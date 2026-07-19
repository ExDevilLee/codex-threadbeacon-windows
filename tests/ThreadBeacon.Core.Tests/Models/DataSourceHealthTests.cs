using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Tests.Models;

public sealed class DataSourceHealthTests
{
    [Fact]
    public void OverallStatus_UsesUnavailableOnlyForTaskDatabase()
    {
        DataSourceHealthReport report = Report(
            rename: DataSourceHealthStatus.Unavailable("Rename 索引不可用"));

        Assert.Equal(OverallDataSourceHealth.Degraded, report.OverallStatus);
        Assert.Equal("部分数据源降级", report.Summary);

        report = Report(
            task: DataSourceHealthStatus.Unavailable("任务数据库不可用"));

        Assert.Equal(OverallDataSourceHealth.Unavailable, report.OverallStatus);
        Assert.Equal("任务数据不可用", report.Summary);
    }

    [Fact]
    public void OverallStatus_IgnoresSourcesThatWereNotUsed()
    {
        DataSourceHealthReport report = Report(
            rollout: DataSourceHealthStatus.NotUsed,
            serviceLogs: DataSourceHealthStatus.NotUsed);

        Assert.Equal(OverallDataSourceHealth.Healthy, report.OverallStatus);
        Assert.Equal("数据源正常", report.Summary);
    }

    [Fact]
    public void WithLastSuccessfulRefresh_PreservesOnlyStableDiagnosticFields()
    {
        DateTimeOffset refreshedAt = DateTimeOffset.FromUnixTimeSeconds(100);
        DataSourceHealthReport report = Report(
            rolloutSuccessCount: 3,
            rolloutFailureCount: 2).WithLastSuccessfulRefresh(refreshedAt);

        Assert.Equal(refreshedAt, report.LastSuccessfulRefreshAt);
        Assert.Equal(3, report.RolloutSuccessCount);
        Assert.Equal(2, report.RolloutFailureCount);
        Assert.DoesNotContain(
            typeof(DataSourceHealthReport).GetProperties(),
            property => property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Error", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Thread", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Constructor_NormalizesNegativeRolloutCounts()
    {
        DataSourceHealthReport report = Report(
            rolloutSuccessCount: -1,
            rolloutFailureCount: -2);

        Assert.Equal(0, report.RolloutSuccessCount);
        Assert.Equal(0, report.RolloutFailureCount);
    }

    private static DataSourceHealthReport Report(
        DataSourceHealthStatus? task = null,
        DataSourceHealthStatus? rename = null,
        DataSourceHealthStatus? rollout = null,
        DataSourceHealthStatus? serviceLogs = null,
        int rolloutSuccessCount = 0,
        int rolloutFailureCount = 0) =>
        new(
            task ?? DataSourceHealthStatus.Healthy,
            rename ?? DataSourceHealthStatus.Healthy,
            rollout ?? DataSourceHealthStatus.Healthy,
            serviceLogs ?? DataSourceHealthStatus.Healthy,
            rolloutSuccessCount,
            rolloutFailureCount,
            null);
}
