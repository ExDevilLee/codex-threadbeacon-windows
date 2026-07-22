using ThreadBeacon.App.ViewModels;
using ThreadBeacon.App.Localization;
using ThreadBeacon.Core.Models;
using System.Windows;
using System.Windows.Media;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class ThreadRowViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void StatusGlyphs_AreDistinctForEverySemanticStatus()
    {
        string[] glyphs = Enum.GetValues<ThreadStatus>()
            .Select(status => new ThreadRowViewModel(
                Snapshot(subagentCount: 0, status: status),
                Now,
                language: AppLanguage.English).StatusGlyph)
            .ToArray();

        Assert.DoesNotContain(glyphs, string.IsNullOrWhiteSpace);
        Assert.Equal(glyphs.Length, glyphs.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void StatusGlyphs_ExistInWindowsFluentIconFont()
    {
        var typeface = new Typeface(
            new FontFamily("Segoe Fluent Icons"),
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal);

        Assert.True(typeface.TryGetGlyphTypeface(out GlyphTypeface? glyphTypeface));
        foreach (ThreadStatus status in Enum.GetValues<ThreadStatus>())
        {
            string glyph = new ThreadRowViewModel(
                Snapshot(subagentCount: 0, status: status),
                Now,
                language: AppLanguage.English).StatusGlyph;
            Assert.True(glyphTypeface.CharacterToGlyphMap.ContainsKey(glyph[0]));
        }
    }

    [Fact]
    public void Constructor_MetadataWithoutTokensStillExposesTaskDetails()
    {
        var viewModel = new ThreadRowViewModel(
            Snapshot(subagentCount: 0, model: "gpt-main", reasoningEffort: "high"),
            Now,
            language: AppLanguage.English);

        Assert.True(viewModel.HasTokenDetails);
        Assert.Equal("gpt-main", viewModel.TokenDetails?.MetadataRows[0].Value);
        Assert.False(viewModel.TokenDetails?.HasTokenUsage);
    }

    [Fact]
    public void Constructor_ZeroCountHidesSubagentPresentation()
    {
        var viewModel = new ThreadRowViewModel(Snapshot(subagentCount: 0), Now);

        Assert.Equal(0, viewModel.SubagentCount);
        Assert.False(viewModel.HasSubagents);
        Assert.Equal(string.Empty, viewModel.SubagentCountText);
        Assert.Equal(string.Empty, viewModel.SubagentAccessibilityLabel);
    }

    [Fact]
    public void Constructor_PositiveCountExposesExactSubagentPresentation()
    {
        var viewModel = new ThreadRowViewModel(
            Snapshot(subagentCount: 3, activeSubagentCount: 2),
            Now);

        Assert.Equal(3, viewModel.SubagentCount);
        Assert.True(viewModel.HasSubagents);
        Assert.Equal("2/3", viewModel.SubagentCountText);
        Assert.Equal("运行中 2 个，共 3 个 Subagent", viewModel.SubagentAccessibilityLabel);
    }

    [Fact]
    public void Constructor_NegativeCountNormalizesToHiddenState()
    {
        var viewModel = new ThreadRowViewModel(Snapshot(subagentCount: -1), Now);

        Assert.Equal(0, viewModel.SubagentCount);
        Assert.False(viewModel.HasSubagents);
    }

    [Fact]
    public void ExpansionState_UpdatesTogglePresentationAndInvokesParentCallback()
    {
        string? toggledId = null;
        var viewModel = new ThreadRowViewModel(
            Snapshot(subagentCount: 3, activeSubagentCount: 2),
            Now,
            id =>
            {
                toggledId = id;
                return Task.CompletedTask;
            });

        Assert.False(viewModel.IsSubagentExpanded);
        Assert.Equal("运行中 2 个，共 3 个 Subagent；点击展开", viewModel.SubagentToggleAccessibilityLabel);

        viewModel.SetSubagentExpanded(true, isLoading: true);
        viewModel.ToggleSubagentsCommand.Execute(null);

        Assert.True(viewModel.IsSubagentExpanded);
        Assert.True(viewModel.IsSubagentLoading);
        Assert.Equal("运行中 2 个，共 3 个 Subagent；点击收起", viewModel.SubagentToggleAccessibilityLabel);
        Assert.Equal("thread-1", toggledId);
    }

    [Fact]
    public void ExpandedEmptyRegion_ReportsLoadingFailureAndEmptyStates()
    {
        var viewModel = new ThreadRowViewModel(Snapshot(subagentCount: 1), Now);

        viewModel.SetSubagentExpanded(true, isLoading: true);
        Assert.True(viewModel.ShowSubagentPlaceholder);
        Assert.Equal("正在读取 Subagent", viewModel.SubagentPlaceholderText);

        viewModel.Update(
            Snapshot(
                subagentCount: 1,
                subagentSourceStatus: ThreadRepositoryStatus.Busy),
            Now);
        viewModel.SetSubagentExpanded(true, isLoading: false);
        Assert.Equal("Subagent 读取失败", viewModel.SubagentPlaceholderText);

        viewModel.Update(Snapshot(subagentCount: 1), Now);
        viewModel.SetSubagentExpanded(true, isLoading: false);
        Assert.Equal("暂无可读取的 Subagent", viewModel.SubagentPlaceholderText);
    }

    [Fact]
    public void Constructor_RetryIncidentShowsServiceDetail()
    {
        var viewModel = new ThreadRowViewModel(
            Snapshot(
                subagentCount: 0,
                serviceIncident: new ServiceIncident(
                    "turn-retry",
                    ServiceIncidentPhase.Retrying,
                    429,
                    2,
                    5,
                    Now.AddSeconds(-10))),
            Now);

        Assert.Equal("服务异常", viewModel.StatusLabel);
        Assert.Equal("HTTP 429 · 重试 2/5", viewModel.IncidentDetailText);
        Assert.True(viewModel.HasIncidentDetail);
    }

    [Fact]
    public void Constructor_FailedIncidentOmitsUnavailableDetail()
    {
        var viewModel = new ThreadRowViewModel(
            Snapshot(
                subagentCount: 0,
                serviceIncident: new ServiceIncident(
                    "turn-failed",
                    ServiceIncidentPhase.Failed,
                    null,
                    null,
                    null,
                    Now.AddSeconds(-10))),
            Now);

        Assert.Equal("服务失败", viewModel.StatusLabel);
        Assert.Equal(string.Empty, viewModel.IncidentDetailText);
        Assert.False(viewModel.HasIncidentDetail);
    }

    [Fact]
    public void Constructor_ModelCapacityIncidentShowsCapacityDetail()
    {
        var viewModel = new ThreadRowViewModel(
            Snapshot(
                subagentCount: 0,
                serviceIncident: new ServiceIncident(
                    "turn-capacity",
                    ServiceIncidentPhase.Failed,
                    null,
                    null,
                    null,
                    Now.AddSeconds(-10),
                    ServiceIncidentKind.ModelCapacity)),
            Now,
            language: AppLanguage.English);

        Assert.Equal("Service failed", viewModel.StatusLabel);
        Assert.Equal("Model at capacity", viewModel.IncidentDetailText);
    }

    [Fact]
    public void Constructor_StreamDisconnectFailureShowsConnectionAndRetryDetail()
    {
        var viewModel = new ThreadRowViewModel(
            Snapshot(
                subagentCount: 0,
                serviceIncident: new ServiceIncident(
                    "turn-disconnect",
                    ServiceIncidentPhase.Failed,
                    null,
                    5,
                    5,
                    Now.AddSeconds(-10),
                    ServiceIncidentKind.StreamDisconnected)),
            Now,
            language: AppLanguage.English);

        Assert.Equal("Service failed", viewModel.StatusLabel);
        Assert.Equal("Connection interrupted · Retry 5/5", viewModel.IncidentDetailText);
    }

    [Fact]
    public void Constructor_NormalTaskHasNoIncidentPresentation()
    {
        var viewModel = new ThreadRowViewModel(Snapshot(subagentCount: 0), Now);

        Assert.Equal("运行中", viewModel.StatusLabel);
        Assert.Equal(string.Empty, viewModel.IncidentDetailText);
        Assert.False(viewModel.HasIncidentDetail);
    }

    [Fact]
    public void Constructor_UsesEnglishPresentationWhenRequested()
    {
        var viewModel = new ThreadRowViewModel(
            Snapshot(subagentCount: 3, activeSubagentCount: 2),
            Now,
            language: AppLanguage.English);

        Assert.Equal("Running", viewModel.StatusLabel);
        Assert.Equal("Pin task", viewModel.PinCommandLabel);
        Assert.Equal("Favorite task", viewModel.FavoriteCommandLabel);
        Assert.Equal("2 running, 3 Subagents total", viewModel.SubagentAccessibilityLabel);
        Assert.Equal("1 min", viewModel.DurationText);
    }

    [Theory]
    [InlineData(ThreadStatus.NeedsAction, "Action")]
    [InlineData(ThreadStatus.JustCompleted, "Done")]
    public void EnglishStatusLabels_RemainCompact(ThreadStatus status, string expected)
    {
        ThreadSnapshot snapshot = Snapshot(subagentCount: 0, status: status);

        var viewModel = new ThreadRowViewModel(snapshot, Now, language: AppLanguage.English);

        Assert.Equal(expected, viewModel.StatusLabel);
    }

    [Fact]
    public void TaskPreferenceCommands_ExposePinStateAndForwardTaskId()
    {
        string? pinnedId = null;
        string? ignoredId = null;
        var viewModel = new ThreadRowViewModel(
            Snapshot(subagentCount: 0),
            Now,
            togglePin: id => pinnedId = id,
            ignore: id => ignoredId = id);

        viewModel.SetPinned(true);
        viewModel.TogglePinCommand.Execute(null);
        viewModel.IgnoreCommand.Execute(null);

        Assert.True(viewModel.IsPinned);
        Assert.Equal("取消置顶", viewModel.PinCommandLabel);
        Assert.Equal("thread-1", pinnedId);
        Assert.Equal("thread-1", ignoredId);
    }

    [Fact]
    public void FavoriteCommand_ExposesStateLabelAndForwardsTaskId()
    {
        string? favoriteId = null;
        var viewModel = new ThreadRowViewModel(
            Snapshot(subagentCount: 0),
            Now,
            toggleFavorite: id => favoriteId = id);

        viewModel.SetFavorite(true);
        viewModel.ToggleFavoriteCommand.Execute(null);

        Assert.True(viewModel.IsFavorite);
        Assert.Equal("取消收藏", viewModel.FavoriteCommandLabel);
        Assert.Equal("thread-1", favoriteId);
    }

    [Fact]
    public void ArchivedSnapshot_UsesNeutralArchivedPresentation()
    {
        var viewModel = new ThreadRowViewModel(
            Snapshot(subagentCount: 0, isArchived: true),
            Now);

        Assert.True(viewModel.IsArchived);
        Assert.Equal("已归档", viewModel.StatusLabel);
        Assert.Equal("#FF8E8E93", viewModel.StatusBrush.ToString());
    }

    private static ThreadSnapshot Snapshot(
        int subagentCount,
        ThreadRepositoryStatus subagentSourceStatus = ThreadRepositoryStatus.Healthy,
        ServiceIncident? serviceIncident = null,
        bool isArchived = false,
        ThreadStatus status = ThreadStatus.Running,
        string? model = null,
        string? reasoningEffort = null,
        int activeSubagentCount = 0) =>
        new(
            "thread-1",
            "Task",
            status,
            Now.AddMinutes(-1),
            Now,
            Now,
            Now.AddMinutes(-1),
            null,
            null,
            subagentCount,
            RolloutSourceStatus.Healthy,
            subagentSourceStatus: subagentSourceStatus,
            serviceIncident: serviceIncident,
            isArchived: isArchived,
            model: model,
            reasoningEffort: reasoningEffort,
            activeSubagentCount: activeSubagentCount);
}
