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

    public ThreadSnapshotLoadResult Load(
        int limit = 8,
        IReadOnlySet<string>? expandedThreadIds = null)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        ThreadLoadResult threadResult = threadRepository.LoadRecent(limit);
        TitleLoadResult titleResult = titleRepository.LoadLatestTitles();
        IReadOnlyList<ThreadRecord> records = ThreadTitleResolver.Resolve(
            threadResult.Threads,
            titleResult.Titles);
        var visibleIds = new HashSet<string>(records.Select(record => record.Id), StringComparer.Ordinal);
        var requestedParentIds = new HashSet<string>(StringComparer.Ordinal);
        if (expandedThreadIds is not null)
        {
            requestedParentIds.UnionWith(expandedThreadIds.Where(visibleIds.Contains));
        }

        SubagentLoadResult subagentResult = requestedParentIds.Count == 0
            ? new SubagentLoadResult(
                ThreadRepositoryStatus.Healthy,
                new Dictionary<string, IReadOnlyList<SubagentRecord>>(StringComparer.Ordinal))
            : threadRepository.LoadDirectSubagents(requestedParentIds);

        ThreadSnapshot[] snapshots = records
            .Select(record => CreateSnapshot(
                record,
                now,
                titleResult.Titles,
                requestedParentIds.Contains(record.Id)
                    ? subagentResult.SubagentsByParent.GetValueOrDefault(record.Id)
                    : null,
                requestedParentIds.Contains(record.Id)
                    ? subagentResult.Status
                    : ThreadRepositoryStatus.Healthy))
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

    private ThreadSnapshot CreateSnapshot(
        ThreadRecord record,
        DateTimeOffset now,
        IReadOnlyDictionary<string, string> titleOverrides,
        IReadOnlyList<SubagentRecord>? subagentRecords,
        ThreadRepositoryStatus subagentSourceStatus)
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

        SubagentSnapshot[] subagents = (subagentRecords ?? Array.Empty<SubagentRecord>())
            .Select(child => CreateSubagentSnapshot(child, titleOverrides, now))
            .OrderBy(child => ThreadStatusPriority.Get(child.Status))
            .ThenByDescending(child => child.LatestEventAt ?? DateTimeOffset.MinValue)
            .ThenBy(child => child.Id, StringComparer.Ordinal)
            .ToArray();

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
            rollout.Status,
            subagents,
            subagentSourceStatus);
    }

    private SubagentSnapshot CreateSubagentSnapshot(
        SubagentRecord record,
        IReadOnlyDictionary<string, string> titleOverrides,
        DateTimeOffset now)
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
        string fallbackTitle = string.IsNullOrWhiteSpace(record.Title)
            ? record.AgentNickname ?? string.Empty
            : record.Title;
        string title = titleOverrides.TryGetValue(record.Id, out string? renamedTitle)
            && !string.IsNullOrWhiteSpace(renamedTitle)
                ? renamedTitle.Trim()
                : fallbackTitle;

        return new SubagentSnapshot(
            record.Id,
            title,
            displayState.Status,
            displayState.ChangedAt,
            record.UpdatedAt,
            rollout.Observation.LatestEventAt,
            tokenUsage,
            record.AgentNickname,
            record.AgentRole,
            record.Model,
            record.ReasoningEffort,
            rollout.Status);
    }
}
