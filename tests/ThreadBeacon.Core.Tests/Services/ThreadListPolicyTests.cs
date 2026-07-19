using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class ThreadListPolicyTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(20_000);

    [Fact]
    public void Evaluate_RestoresOnlyAfterStrictlyNewerTaskStart()
    {
        var preferences = new ThreadListPreferences(
            ignoredRules: new Dictionary<string, IgnoredThreadRule>(StringComparer.Ordinal)
            {
                ["same"] = new("same", Now, ThreadIgnoreMode.UntilNextTurn),
                ["newer"] = new("newer", Now, ThreadIgnoreMode.UntilNextTurn),
            });

        ThreadListResult result = ThreadListPolicy.Evaluate(
            [
                Snapshot("same", ThreadStatus.Running, taskStartedAt: Now),
                Snapshot("newer", ThreadStatus.Running, taskStartedAt: Now.AddSeconds(1)),
            ],
            preferences,
            limit: 8);

        Assert.Equal(["newer"], result.VisibleSnapshots.Select(thread => thread.Id));
        Assert.Equal(["same"], result.IgnoredSnapshots.Select(thread => thread.Id));
        Assert.Equal(["same"], result.Preferences.IgnoredRules.Keys);
    }

    [Fact]
    public void Evaluate_KeepsStatusAheadOfPinAndPinAheadOfRecency()
    {
        var preferences = new ThreadListPreferences(pinnedThreadIds: ["pinned-idle", "older-running"]);

        ThreadListResult result = ThreadListPolicy.Evaluate(
            [
                Snapshot("pinned-idle", ThreadStatus.Idle, eventAt: Now.AddMinutes(3)),
                Snapshot("newer-running", ThreadStatus.Running, eventAt: Now.AddMinutes(2)),
                Snapshot("older-running", ThreadStatus.Running, eventAt: Now),
            ],
            preferences,
            limit: 8);

        Assert.Equal(
            ["older-running", "newer-running", "pinned-idle"],
            result.VisibleSnapshots.Select(thread => thread.Id));
    }

    [Fact]
    public void Evaluate_LimitsVisibleRowsAfterRemovingIgnoredCandidates()
    {
        ThreadSnapshot[] candidates = Enumerable.Range(0, 10)
            .Select(index => Snapshot($"task-{index}", ThreadStatus.Idle, Now.AddSeconds(index)))
            .Append(Snapshot("ignored", ThreadStatus.Error, Now.AddMinutes(1)))
            .ToArray();
        var preferences = new ThreadListPreferences(
            ignoredRules: new Dictionary<string, IgnoredThreadRule>(StringComparer.Ordinal)
            {
                ["ignored"] = new("ignored", Now, ThreadIgnoreMode.UntilNextTurn),
            });

        ThreadListResult result = ThreadListPolicy.Evaluate(candidates, preferences, limit: 8);

        Assert.Equal(8, result.VisibleSnapshots.Count);
        Assert.Equal(["ignored"], result.IgnoredSnapshots.Select(thread => thread.Id));
        Assert.DoesNotContain(result.VisibleSnapshots, thread => thread.Id == "ignored");
    }

    [Fact]
    public void Evaluate_UsesOrdinalTaskIdAsFinalTieBreaker()
    {
        ThreadListResult result = ThreadListPolicy.Evaluate(
            [Snapshot("b", ThreadStatus.Idle), Snapshot("A", ThreadStatus.Idle), Snapshot("a", ThreadStatus.Idle)],
            new ThreadListPreferences(),
            limit: 8);

        Assert.Equal(["A", "a", "b"], result.VisibleSnapshots.Select(thread => thread.Id));
    }

    [Fact]
    public void Evaluate_DoesNotMutateInputPreferences()
    {
        var preferences = new ThreadListPreferences(
            pinnedThreadIds: ["pinned"],
            ignoredRules: new Dictionary<string, IgnoredThreadRule>(StringComparer.Ordinal)
            {
                ["ignored"] = new("ignored", Now, ThreadIgnoreMode.UntilNextTurn),
            });

        _ = ThreadListPolicy.Evaluate(
            [Snapshot("ignored", ThreadStatus.Running, taskStartedAt: Now.AddSeconds(1))],
            preferences,
            limit: 8);

        Assert.Contains("pinned", preferences.PinnedThreadIds);
        Assert.Contains("ignored", preferences.IgnoredRules.Keys);
    }

    [Fact]
    public void Evaluate_FavoritesOnlyFiltersWithoutChangingNormalSortOrder()
    {
        ThreadSnapshot[] candidates =
        [
            Snapshot("running", ThreadStatus.Running, Now.AddMinutes(1)),
            Snapshot("favorite-idle", ThreadStatus.Idle, Now.AddMinutes(2)),
            Snapshot("newer-idle", ThreadStatus.Idle, Now.AddMinutes(3)),
        ];
        var allPreferences = new ThreadListPreferences(favoriteThreadIds: ["favorite-idle"]);
        var favoritePreferences = new ThreadListPreferences(
            favoriteThreadIds: ["favorite-idle"],
            showsFavoritesOnly: true);

        ThreadListResult all = ThreadListPolicy.Evaluate(candidates, allPreferences, limit: 8);
        ThreadListResult favorites = ThreadListPolicy.Evaluate(candidates, favoritePreferences, limit: 8);

        Assert.Equal(["running", "newer-idle", "favorite-idle"], all.VisibleSnapshots.Select(x => x.Id));
        Assert.Equal(["favorite-idle"], favorites.VisibleSnapshots.Select(x => x.Id));
    }

    [Fact]
    public void Evaluate_IgnoreWinsAndMissingFavoriteRemainsPersisted()
    {
        var preferences = new ThreadListPreferences(
            favoriteThreadIds: ["favorite", "missing"],
            showsFavoritesOnly: true,
            ignoredRules: new Dictionary<string, IgnoredThreadRule>(StringComparer.Ordinal)
            {
                ["favorite"] = new("favorite", Now, ThreadIgnoreMode.UntilNextTurn),
            });

        ThreadListResult result = ThreadListPolicy.Evaluate(
            [Snapshot("favorite", ThreadStatus.Running)],
            preferences,
            limit: 8);

        Assert.Empty(result.VisibleSnapshots);
        Assert.Equal(["favorite"], result.IgnoredSnapshots.Select(x => x.Id));
        Assert.True(result.Preferences.FavoriteThreadIds.SetEquals(["favorite", "missing"]));
    }

    private static ThreadSnapshot Snapshot(
        string id,
        ThreadStatus status,
        DateTimeOffset? eventAt = null,
        DateTimeOffset? taskStartedAt = null) =>
        new(
            id,
            id,
            status,
            eventAt ?? Now,
            eventAt ?? Now,
            eventAt ?? Now,
            taskStartedAt,
            null,
            null,
            0,
            RolloutSourceStatus.Healthy);
}
