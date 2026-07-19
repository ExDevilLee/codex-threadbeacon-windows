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
    private readonly ILogEventRepository? logEventRepository;

    public ThreadStatusLoader(
        IThreadRepository threadRepository,
        ISessionIndexTitleRepository titleRepository,
        IRolloutTailParser rolloutParser,
        TimeProvider? timeProvider = null,
        TimeSpan? completedRetention = null,
        TimeSpan? runningFreshness = null,
        ILogEventRepository? logEventRepository = null)
    {
        this.threadRepository = threadRepository ?? throw new ArgumentNullException(nameof(threadRepository));
        this.titleRepository = titleRepository ?? throw new ArgumentNullException(nameof(titleRepository));
        this.rolloutParser = rolloutParser ?? throw new ArgumentNullException(nameof(rolloutParser));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.completedRetention = completedRetention ?? TimeSpan.FromSeconds(60);
        this.runningFreshness = runningFreshness ?? TimeSpan.FromSeconds(120);
        this.logEventRepository = logEventRepository;
    }

    public ThreadSnapshotLoadResult Load(
        int limit = 8,
        IReadOnlySet<string>? expandedThreadIds = null)
    {
        return Load(new ThreadLoadRequest(
            limit,
            new HashSet<string>(StringComparer.Ordinal),
            expandedThreadIds ?? new HashSet<string>(StringComparer.Ordinal)));
    }

    public ThreadSnapshotLoadResult Load(ThreadLoadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.RecentLimit, 1);
        DateTimeOffset now = timeProvider.GetUtcNow();
        ThreadLoadResult recentResult = threadRepository.LoadRecent(request.RecentLimit);
        ThreadLoadResult includedResult = request.IncludedThreadIds.Count == 0
            ? new ThreadLoadResult(ThreadRepositoryStatus.Healthy, [])
            : threadRepository.LoadByIds(request.IncludedThreadIds);
        var recordsById = new Dictionary<string, ThreadRecord>(StringComparer.Ordinal);
        foreach (ThreadRecord record in recentResult.Threads.Concat(includedResult.Threads))
        {
            recordsById[record.Id] = record;
        }

        ThreadRepositoryStatus threadStatus = recentResult.Status is ThreadRepositoryStatus.Healthy
            ? includedResult.Status
            : recentResult.Status;
        TitleLoadResult titleResult = titleRepository.LoadLatestTitles();
        IReadOnlyList<ThreadRecord> records = ThreadTitleResolver.Resolve(
            recordsById.Values.ToArray(),
            titleResult.Titles);
        var visibleIds = new HashSet<string>(records.Select(record => record.Id), StringComparer.Ordinal);
        IReadOnlyDictionary<string, ServiceIncident> incidents = LoadIncidents(visibleIds);
        var requestedParentIds = new HashSet<string>(StringComparer.Ordinal);
        requestedParentIds.UnionWith(request.ExpandedThreadIds.Where(visibleIds.Contains));

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
                incidents.GetValueOrDefault(record.Id),
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
            threadStatus,
            titleResult.Status,
            snapshots,
            now);
    }

    private ThreadSnapshot CreateSnapshot(
        ThreadRecord record,
        DateTimeOffset now,
        IReadOnlyDictionary<string, string> titleOverrides,
        ServiceIncident? incident,
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
        incident = ClearResolvedIncident(incident, rollout.Observation);
        ThreadStatus status = incident?.Phase switch
        {
            ServiceIncidentPhase.Retrying => ThreadStatus.Warning,
            ServiceIncidentPhase.Failed => ThreadStatus.Error,
            _ => displayState.Status,
        };
        DateTimeOffset statusChangedAt = incident?.OccurredAt ?? displayState.ChangedAt;
        DateTimeOffset? latestEventAt = Later(
            rollout.Observation.LatestEventAt,
            incident?.OccurredAt);
        DateTimeOffset? completionEventAt = incident is null
            ? rollout.Observation.CompletionEventAt
            : null;
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
            status,
            statusChangedAt,
            record.UpdatedAt,
            latestEventAt,
            rollout.Observation.LatestTaskStartedAt,
            completionEventAt,
            tokenUsage,
            record.SubagentCount,
            rollout.Status,
            subagents,
            subagentSourceStatus,
            incident);
    }

    private IReadOnlyDictionary<string, ServiceIncident> LoadIncidents(
        IReadOnlySet<string> visibleIds)
    {
        if (logEventRepository is null || visibleIds.Count == 0)
        {
            return new Dictionary<string, ServiceIncident>(StringComparer.Ordinal);
        }

        try
        {
            return logEventRepository.LoadLatestIncidents(visibleIds);
        }
        catch (Exception)
        {
            return new Dictionary<string, ServiceIncident>(StringComparer.Ordinal);
        }
    }

    private static ServiceIncident? ClearResolvedIncident(
        ServiceIncident? incident,
        RolloutObservation observation)
    {
        if (incident is null
            || observation.LatestTaskStartedAt is DateTimeOffset startedAt
                && startedAt > incident.OccurredAt
            || incident.Phase is ServiceIncidentPhase.Retrying
                && observation.CompletionEventAt is DateTimeOffset completedAt
                && completedAt > incident.OccurredAt)
        {
            return null;
        }

        return incident;
    }

    private static DateTimeOffset? Later(DateTimeOffset? left, DateTimeOffset? right) =>
        left is null ? right : right is null ? left : left > right ? left : right;

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
