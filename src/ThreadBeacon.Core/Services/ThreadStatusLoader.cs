using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public sealed class ThreadStatusLoader
{
    private readonly IThreadRepository threadRepository;
    private readonly ISessionIndexTitleRepository titleRepository;
    private readonly IRolloutTailParser rolloutParser;
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan completedRetention;
    private readonly TimeSpan runningFreshness;

    public ThreadStatusLoader(
        IThreadRepository threadRepository,
        ISessionIndexTitleRepository titleRepository,
        IRolloutTailParser rolloutParser,
        TimeProvider? timeProvider = null,
        TimeSpan? completedRetention = null,
        TimeSpan? runningFreshness = null)
    {
        this.threadRepository = threadRepository ?? throw new ArgumentNullException(nameof(threadRepository));
        this.titleRepository = titleRepository ?? throw new ArgumentNullException(nameof(titleRepository));
        this.rolloutParser = rolloutParser ?? throw new ArgumentNullException(nameof(rolloutParser));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.completedRetention = completedRetention ?? TimeSpan.FromSeconds(60);
        this.runningFreshness = runningFreshness ?? TimeSpan.FromSeconds(120);
    }

    public ThreadSnapshotLoadResult Load(int limit = 8)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        ThreadLoadResult threadResult = threadRepository.LoadRecent(limit);
        TitleLoadResult titleResult = titleRepository.LoadLatestTitles();
        IReadOnlyList<ThreadRecord> records = ThreadTitleResolver.Resolve(
            threadResult.Threads,
            titleResult.Titles);

        ThreadSnapshot[] snapshots = records
            .Select(record => CreateSnapshot(record, now))
            .OrderBy(snapshot => ThreadStatusPriority.Get(snapshot.Status))
            .ThenByDescending(snapshot => snapshot.LatestEventAt ?? DateTimeOffset.MinValue)
            .ThenBy(snapshot => snapshot.Id, StringComparer.Ordinal)
            .ToArray();

        return new ThreadSnapshotLoadResult(
            threadResult.Status,
            titleResult.Status,
            snapshots,
            now);
    }

    private ThreadSnapshot CreateSnapshot(ThreadRecord record, DateTimeOffset now)
    {
        RolloutLoadResult rollout = rolloutParser.Parse(record.RolloutPath);
        ThreadDisplayState displayState = ThreadStatusPolicy.Evaluate(
            rollout.Observation,
            record.UpdatedAt,
            now,
            completedRetention,
            runningFreshness);
        TokenUsageSnapshot? tokenUsage = rollout.Observation.TokenUsage
            ?? (record.TokensUsed > 0
                ? new TokenUsageSnapshot(record.TokensUsed, null, null, null)
                : null);

        return new ThreadSnapshot(
            record.Id,
            record.Title,
            displayState.Status,
            displayState.ChangedAt,
            record.UpdatedAt,
            rollout.Observation.LatestEventAt,
            rollout.Observation.LatestTaskStartedAt,
            rollout.Observation.CompletionEventAt,
            tokenUsage,
            record.SubagentCount,
            rollout.Status);
    }
}
