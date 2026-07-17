using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class ThreadStatusLoaderTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(10_000);

    [Fact]
    public void Load_MergesSourcesAndSortsByStatusPriority()
    {
        ThreadRecord[] records =
        [
            Record("missing", "Missing rollout", tokens: 50),
            Record("idle", "Idle"),
            Record("completed", "Completed"),
            Record("running", "Original running title", tokens: 999),
        ];
        var observations = new Dictionary<string, RolloutLoadResult>
        {
            ["running"] = HealthyObservation(
                ThreadStatus.Running,
                Now.AddSeconds(-10),
                Now.AddSeconds(-5),
                new TokenUsageSnapshot(1_000, null, null, Now.AddSeconds(-5))),
            ["completed"] = HealthyObservation(
                ThreadStatus.JustCompleted,
                Now.AddSeconds(-30),
                Now.AddSeconds(-30)),
            ["idle"] = HealthyObservation(
                ThreadStatus.JustCompleted,
                Now.AddSeconds(-120),
                Now.AddSeconds(-120)),
            ["missing"] = new(RolloutSourceStatus.Missing, RolloutObservation.Empty),
        };
        ThreadStatusLoader loader = CreateLoader(
            records,
            new Dictionary<string, string> { ["running"] = "Renamed running task" },
            observations);

        ThreadSnapshotLoadResult result = loader.Load();

        Assert.Equal(
            [ThreadStatus.Running, ThreadStatus.JustCompleted, ThreadStatus.Idle, ThreadStatus.Unknown],
            result.Threads.Select(thread => thread.Status));
        Assert.Equal("Renamed running task", result.Threads[0].Title);
        Assert.Equal(1_000, result.Threads[0].TokenUsage?.TotalTokens);
        Assert.Equal(50, result.Threads[^1].TokenUsage?.TotalTokens);
        Assert.Equal(RolloutSourceStatus.Missing, result.Threads[^1].RolloutSourceStatus);
        Assert.False(result.IsHealthy);
    }

    [Fact]
    public void Load_SortsEqualStatusesByLatestEventThenId()
    {
        ThreadRecord[] records =
        [
            Record("b", "B"),
            Record("a", "A"),
        ];
        var observations = new Dictionary<string, RolloutLoadResult>
        {
            ["a"] = HealthyObservation(ThreadStatus.Running, Now, Now),
            ["b"] = HealthyObservation(ThreadStatus.Running, Now, Now.AddSeconds(-1)),
        };
        ThreadStatusLoader loader = CreateLoader(records, new Dictionary<string, string>(), observations);

        ThreadSnapshotLoadResult result = loader.Load();

        Assert.Equal(["a", "b"], result.Threads.Select(thread => thread.Id));
    }

    [Fact]
    public void Load_PropagatesDataSourceHealthWithoutDroppingThreads()
    {
        var threadRepository = new StubThreadRepository(new ThreadLoadResult(
            ThreadRepositoryStatus.Healthy,
            [Record("thread", "SQLite title")]));
        var titleRepository = new StubTitleRepository(new TitleLoadResult(
            SessionIndexStatus.Missing,
            new Dictionary<string, string>()));
        var rolloutParser = new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
        {
            ["thread"] = HealthyObservation(ThreadStatus.Running, Now, Now),
        });
        var loader = new ThreadStatusLoader(
            threadRepository,
            titleRepository,
            rolloutParser,
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load();

        ThreadSnapshot snapshot = Assert.Single(result.Threads);
        Assert.Equal("SQLite title", snapshot.Title);
        Assert.Equal(SessionIndexStatus.Missing, result.TitleSourceStatus);
        Assert.False(result.IsHealthy);
    }

    private static ThreadStatusLoader CreateLoader(
        IReadOnlyList<ThreadRecord> records,
        IReadOnlyDictionary<string, string> titles,
        IReadOnlyDictionary<string, RolloutLoadResult> observations) =>
        new(
            new StubThreadRepository(new ThreadLoadResult(ThreadRepositoryStatus.Healthy, records)),
            new StubTitleRepository(new TitleLoadResult(SessionIndexStatus.Healthy, titles)),
            new StubRolloutParser(observations),
            new FixedTimeProvider(Now));

    private static ThreadRecord Record(
        string id,
        string title,
        long tokens = 0) =>
        new(id, title, id, Now.AddMinutes(-5), tokens, 0);

    private static RolloutLoadResult HealthyObservation(
        ThreadStatus status,
        DateTimeOffset changedAt,
        DateTimeOffset latestEventAt,
        TokenUsageSnapshot? tokenUsage = null) =>
        new(
            RolloutSourceStatus.Healthy,
            new RolloutObservation(status, changedAt, latestEventAt, null, null, tokenUsage));

    private sealed class StubThreadRepository(ThreadLoadResult result) : IThreadRepository
    {
        public ThreadLoadResult LoadRecent(int limit = 8) => result;
    }

    private sealed class StubTitleRepository(TitleLoadResult result) : ISessionIndexTitleRepository
    {
        public TitleLoadResult LoadLatestTitles() => result;
    }

    private sealed class StubRolloutParser(
        IReadOnlyDictionary<string, RolloutLoadResult> observations) : IRolloutTailParser
    {
        public RolloutLoadResult Parse(string filePath) => observations[filePath];
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
