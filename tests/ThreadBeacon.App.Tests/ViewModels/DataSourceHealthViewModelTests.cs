using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class DataSourceHealthViewModelTests
{
    [Fact]
    public void Update_ExposesStablePrivacySafePresentation()
    {
        DateTimeOffset refreshedAt = new(2026, 7, 19, 8, 20, 0, TimeSpan.Zero);
        var viewModel = new DataSourceHealthViewModel();
        DataSourceHealthRowViewModel[] sourceRows = viewModel.Sources.ToArray();

        viewModel.Update(new DataSourceHealthReport(
            DataSourceHealthStatus.Healthy,
            DataSourceHealthStatus.Unavailable("未找到 Rename 索引"),
            DataSourceHealthStatus.Degraded("部分 Rollout 无法读取"),
            DataSourceHealthStatus.Healthy,
            3,
            2,
            refreshedAt));

        Assert.Equal(OverallDataSourceHealth.Degraded, viewModel.OverallStatus);
        Assert.Equal("部分数据源降级", viewModel.Summary);
        Assert.Equal("数据源健康：部分数据源降级", viewModel.AccessibilityLabel);
        Assert.Equal("成功 3 | 失败 2", viewModel.RolloutCountsText);
        Assert.Equal(
            $"最后成功刷新：{refreshedAt.ToLocalTime():HH:mm:ss}",
            viewModel.LastSuccessfulRefreshText);
        Assert.Equal(
            ["任务数据库", "Rename 索引", "Rollout", "服务日志"],
            viewModel.Sources.Select(source => source.Title));
        Assert.Equal(sourceRows, viewModel.Sources);
        Assert.Equal("未找到 Rename 索引", viewModel.Sources[1].DetailText);
        Assert.DoesNotContain(
            viewModel.Sources,
            source => source.DetailText.Contains("\\", StringComparison.Ordinal));
    }

    [Fact]
    public void Update_FormatsMissingSuccessfulRefreshAndUnusedSources()
    {
        var viewModel = new DataSourceHealthViewModel();

        viewModel.Update(new DataSourceHealthReport(
            DataSourceHealthStatus.NotUsed,
            DataSourceHealthStatus.NotUsed,
            DataSourceHealthStatus.NotUsed,
            DataSourceHealthStatus.NotUsed,
            0,
            0,
            null));

        Assert.Equal("尚无成功刷新记录", viewModel.LastSuccessfulRefreshText);
        Assert.Equal(string.Empty, viewModel.RolloutCountsText);
        Assert.All(
            viewModel.Sources,
            source => Assert.Equal(DataSourceHealthLevel.NotUsed, source.Level));
    }

    [Fact]
    public void HealthButtonGlyphs_MatchMacOsOverallStateMapping()
    {
        var viewModel = new DataSourceHealthViewModel();

        Assert.Equal("\uEA18", viewModel.HealthButtonBaseGlyph);
        Assert.Equal("\uE73E", viewModel.HealthButtonOverlayGlyph);

        viewModel.Update(new DataSourceHealthReport(
            DataSourceHealthStatus.Healthy,
            DataSourceHealthStatus.Degraded("Rename unavailable"),
            DataSourceHealthStatus.Healthy,
            DataSourceHealthStatus.Healthy,
            1,
            0,
            DateTimeOffset.Now));

        Assert.Equal("\uE7BA", viewModel.HealthButtonBaseGlyph);
        Assert.Equal(string.Empty, viewModel.HealthButtonOverlayGlyph);

        viewModel.Update(new DataSourceHealthReport(
            DataSourceHealthStatus.Unavailable("Database unavailable"),
            DataSourceHealthStatus.NotUsed,
            DataSourceHealthStatus.NotUsed,
            DataSourceHealthStatus.NotUsed,
            0,
            0,
            null));

        Assert.Equal("\uEA39", viewModel.HealthButtonBaseGlyph);
        Assert.Equal(string.Empty, viewModel.HealthButtonOverlayGlyph);
    }
}
