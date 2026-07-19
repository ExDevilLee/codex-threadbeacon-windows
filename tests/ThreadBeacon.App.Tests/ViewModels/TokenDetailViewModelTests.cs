using ThreadBeacon.App.ViewModels;
using ThreadBeacon.App.Localization;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class TokenDetailViewModelTests
{
    [Fact]
    public void Constructor_MapsFieldsInMacCompatibleOrder()
    {
        var updatedAt = new DateTimeOffset(2026, 7, 18, 12, 34, 56, TimeSpan.Zero);
        var snapshot = new TokenUsageSnapshot(
            3_200,
            new TokenUsage(2_000, 800, 300, 100, 3_200),
            new TokenUsage(300, 100, 80, 20, 500),
            updatedAt);

        var details = new TokenDetailViewModel(snapshot);

        Assert.Equal(
            ["会话总量", "输入", "缓存输入", "非缓存输入", "输出", "Reasoning", "当前 turn", "缓存率", "更新时间"],
            details.Rows.Select(row => row.Label));
        Assert.Equal(
            ["3.2K", "2K", "800", "1.2K", "300", "100", "+500", "40%", updatedAt.ToLocalTime().ToString("HH:mm:ss")],
            details.Rows.Select(row => row.Value));
        Assert.Equal("缓存输入已包含在输入中；Reasoning 已包含在输出中。", details.Note);
    }

    [Fact]
    public void Constructor_WithTotalOnly_UsesDashesForUnavailableFields()
    {
        var details = new TokenDetailViewModel(new TokenUsageSnapshot(900, null, null, null));

        Assert.Equal("900", details.Rows[0].Value);
        Assert.All(details.Rows.Skip(1), row => Assert.Equal("—", row.Value));
    }

    [Fact]
    public void Constructor_UsesEnglishLabelsAndNoteWhenRequested()
    {
        var details = new TokenDetailViewModel(
            new TokenUsageSnapshot(900, null, null, null),
            AppLanguage.English);

        Assert.Equal(
            ["Session total", "Input", "Cached input", "Uncached input", "Output", "Reasoning", "Current turn", "Cache ratio", "Updated"],
            details.Rows.Select(row => row.Label));
        Assert.Equal("Cached input is included in input; Reasoning is included in output.", details.Note);
    }
}
