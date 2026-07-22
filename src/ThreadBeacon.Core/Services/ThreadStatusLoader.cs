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
        IReadOnlySet<string> favoriteThreadIds = request.FavoriteThreadIds
            ?? new HashSet<string>(StringComparer.Ordinal);
        ThreadLoadResult favoriteResult = favoriteThreadIds.Count == 0
            ? new ThreadLoadResult(ThreadRepositoryStatus.Healthy, [])
            : threadRepository.LoadByIdsIncludingArchived(favoriteThreadIds);
        TitleLoadResult titleResult = titleRepository.LoadLatestTitles();
        bool detachedCandidatesWereUsed = titleResult.IsHealthy && titleResult.Titles.Count > 0;
        ThreadLoadResult detachedCandidateResult = detachedCandidatesWereUsed
            ? threadRepository.LoadDetachedSubagentCandidates(request.RecentLimit)
            : new ThreadLoadResult(ThreadRepositoryStatus.Healthy, []);
        IEnumerable<ThreadRecord> promotedDetachedRecords = detachedCandidateResult.Threads
            .Where(record => titleResult.Titles.TryGetValue(record.Id, out string? title)
                && !string.IsNullOrWhiteSpace(title));
        var recordsById = new Dictionary<string, ThreadRecord>(StringComparer.Ordinal);
        foreach (ThreadRecord record in recentResult.Threads
            .Concat(includedResult.Threads)
            .Concat(favoriteResult.Threads)
            .Concat(promotedDetachedRecords))
        {
            recordsById[record.Id] = record;
        }

        IReadOnlyList<ThreadRecord> records = ThreadTitleResolver.Resolve(
            recordsById.Values.ToArray(),
            titleResult.Titles);
        var activeVisibleIds = new HashSet<string>(
            records.Where(record => !record.IsArchived).Select(record => record.Id),
            StringComparer.Ordinal);
        var activityParentIds = new HashSet<string>(
            records
                .Where(record => !record.IsArchived && record.SubagentCount > 0)
                .Select(record => record.Id),
            StringComparer.Ordinal);
        SubagentActivityLoadResult activityResult = activityParentIds.Count == 0
            ? EmptySubagentActivityResult(ThreadRepositoryStatus.Healthy)
            : threadRepository.LoadRecentSubagentCandidates(
                activityParentIds,
                now - runningFreshness);
        var activityRollouts = new Dictionary<string, RolloutLoadResult>(StringComparer.Ordinal);
        var activeSubagentCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach ((string parentId, IReadOnlyList<SubagentActivityCandidate> candidates)
            in activityResult.CandidatesByParent)
        {
            int activeCount = 0;
            foreach (SubagentActivityCandidate candidate in candidates)
            {
                RolloutLoadResult childRollout = rolloutParser.Parse(candidate.RolloutPath);
                activityRollouts[candidate.Id] = childRollout;
                ThreadDisplayState childState = ThreadStatusPolicy.Evaluate(
                    childRollout.Observation,
                    candidate.UpdatedAt,
                    now,
                    completedRetention,
                    runningFreshness);
                if (childState.Status is ThreadStatus.Running)
                {
                    activeCount++;
                }
            }

            activeSubagentCounts[parentId] = activeCount;
        }

        ThreadRepositoryStatus threadStatus = FirstUnhealthyStatus(
            recentResult.Status,
            includedResult.Status,
            favoriteResult.Status,
            detachedCandidateResult.Status);
        ServiceLogLoadResult incidentResult = LoadIncidents(activeVisibleIds);
        IReadOnlyDictionary<string, ServiceIncident> incidents = incidentResult.Incidents;
        var requestedParentIds = new HashSet<string>(StringComparer.Ordinal);
        requestedParentIds.UnionWith(request.ExpandedThreadIds.Where(activeVisibleIds.Contains));

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
                    : ThreadRepositoryStatus.Healthy,
                activeSubagentCounts.GetValueOrDefault(record.Id),
                activityRollouts))
            .OrderBy(snapshot => ThreadStatusPriority.Get(snapshot.Status))
            .ThenByDescending(snapshot => snapshot.LatestEventAt ?? DateTimeOffset.MinValue)
            .ThenBy(snapshot => snapshot.Id, StringComparer.Ordinal)
            .ToArray();

        DataSourceHealthReport health = CreateHealthReport(
            recentResult.Status,
            includedResult.Status,
            request.IncludedThreadIds.Count > 0,
            favoriteResult.Status,
            favoriteThreadIds.Count > 0,
            detachedCandidateResult.Status,
            detachedCandidatesWereUsed,
            subagentResult.Status,
            requestedParentIds.Count > 0,
            activityResult.Status,
            activityParentIds.Count > 0,
            titleResult.Status,
            snapshots,
            incidentResult.Status,
            activityRollouts);

        return new ThreadSnapshotLoadResult(
            threadStatus,
            titleResult.Status,
            snapshots,
            now,
            health);
    }

    private ThreadSnapshot CreateSnapshot(
        ThreadRecord record,
        DateTimeOffset now,
        IReadOnlyDictionary<string, string> titleOverrides,
        ServiceIncident? incident,
        IReadOnlyList<SubagentRecord>? subagentRecords,
        ThreadRepositoryStatus subagentSourceStatus,
        int activeSubagentCount,
        IReadOnlyDictionary<string, RolloutLoadResult> activityRollouts)
    {
        RolloutLoadResult rollout = rolloutParser.Parse(record.RolloutPath);
        ThreadDisplayState displayState = ThreadStatusPolicy.Evaluate(
            rollout.Observation,
            record.UpdatedAt,
            now,
            completedRetention,
            runningFreshness);
        incident = record.IsArchived
            ? null
            : ClearResolvedIncident(incident, rollout.Observation);
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
        DateTimeOffset? latestTaskStartedAt = rollout.Observation.LatestTaskStartedAt;
        DateTimeOffset? completionEventAt = incident is null
            ? rollout.Observation.CompletionEventAt
            : null;
        if (record.IsArchived)
        {
            status = ThreadStatus.Idle;
            statusChangedAt = record.UpdatedAt;
            latestEventAt = record.UpdatedAt;
            latestTaskStartedAt = null;
            completionEventAt = null;
        }
        TokenUsageSnapshot? tokenUsage = rollout.Observation.TokenUsage
            ?? (record.TokensUsed > 0
                ? new TokenUsageSnapshot(record.TokensUsed, null, null, null)
                : null);

        SubagentSnapshot[] subagents = (subagentRecords ?? Array.Empty<SubagentRecord>())
            .Select(child => CreateSubagentSnapshot(
                child,
                titleOverrides,
                now,
                activityRollouts.GetValueOrDefault(child.Id)))
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
            latestTaskStartedAt,
            completionEventAt,
            tokenUsage,
            record.IsArchived ? 0 : record.SubagentCount,
            rollout.Status,
            subagents,
            subagentSourceStatus,
            incident,
            record.IsArchived,
            record.RolloutPath,
            record.Model ?? rollout.Observation.Model,
            record.ReasoningEffort ?? rollout.Observation.ReasoningEffort,
            record.IsArchived ? 0 : activeSubagentCount);
    }

    private ServiceLogLoadResult LoadIncidents(
        IReadOnlySet<string> visibleIds)
    {
        if (logEventRepository is null || visibleIds.Count == 0)
        {
            return EmptyServiceLogResult(ServiceLogSourceStatus.NotUsed);
        }

        try
        {
            return logEventRepository.LoadLatestIncidents(visibleIds);
        }
        catch (Exception)
        {
            return EmptyServiceLogResult(ServiceLogSourceStatus.Unavailable);
        }
    }

    private static DataSourceHealthReport CreateHealthReport(
        ThreadRepositoryStatus recentStatus,
        ThreadRepositoryStatus includedStatus,
        bool includedWasUsed,
        ThreadRepositoryStatus favoriteStatus,
        bool favoriteWasUsed,
        ThreadRepositoryStatus detachedCandidateStatus,
        bool detachedCandidatesWereUsed,
        ThreadRepositoryStatus subagentStatus,
        bool subagentWasUsed,
        ThreadRepositoryStatus activityStatus,
        bool activityWasUsed,
        SessionIndexStatus titleStatus,
        IReadOnlyList<ThreadSnapshot> snapshots,
        ServiceLogSourceStatus serviceLogStatus,
        IReadOnlyDictionary<string, RolloutLoadResult> activityRollouts)
    {
        var activeIds = new HashSet<string>(activityRollouts.Keys, StringComparer.Ordinal);
        RolloutSourceStatus[] rolloutStatuses = snapshots
            .SelectMany(snapshot => snapshot.Subagents
                .Where(subagent => !activeIds.Contains(subagent.Id))
                .Select(subagent => subagent.RolloutSourceStatus)
                .Prepend(snapshot.RolloutSourceStatus))
            .Concat(activityRollouts.Values.Select(result => result.Status))
            .ToArray();
        int rolloutSuccessCount = rolloutStatuses.Count(
            status => status is RolloutSourceStatus.Healthy);
        int rolloutFailureCount = rolloutStatuses.Length - rolloutSuccessCount;

        return new DataSourceHealthReport(
            TaskDatabaseHealth(
                recentStatus,
                includedStatus,
                includedWasUsed,
                favoriteStatus,
                favoriteWasUsed,
                detachedCandidateStatus,
                detachedCandidatesWereUsed,
                subagentStatus,
                subagentWasUsed,
                activityStatus,
                activityWasUsed),
            RenameHealth(titleStatus),
            RolloutHealth(rolloutSuccessCount, rolloutFailureCount),
            ServiceLogHealth(serviceLogStatus),
            rolloutSuccessCount,
            rolloutFailureCount,
            null);
    }

    private static DataSourceHealthStatus TaskDatabaseHealth(
        ThreadRepositoryStatus recentStatus,
        ThreadRepositoryStatus includedStatus,
        bool includedWasUsed,
        ThreadRepositoryStatus favoriteStatus,
        bool favoriteWasUsed,
        ThreadRepositoryStatus detachedCandidateStatus,
        bool detachedCandidatesWereUsed,
        ThreadRepositoryStatus subagentStatus,
        bool subagentWasUsed,
        ThreadRepositoryStatus activityStatus,
        bool activityWasUsed)
    {
        if (recentStatus is not ThreadRepositoryStatus.Healthy)
        {
            return DataSourceHealthStatus.Unavailable(ThreadRepositoryDetail(recentStatus));
        }

        ThreadRepositoryStatus[] usedSupplementalStatuses =
        [
            includedWasUsed ? includedStatus : ThreadRepositoryStatus.Healthy,
            favoriteWasUsed ? favoriteStatus : ThreadRepositoryStatus.Healthy,
            detachedCandidatesWereUsed
                ? detachedCandidateStatus
                : ThreadRepositoryStatus.Healthy,
            subagentWasUsed ? subagentStatus : ThreadRepositoryStatus.Healthy,
            activityWasUsed ? activityStatus : ThreadRepositoryStatus.Healthy,
        ];
        ThreadRepositoryStatus supplementalFailure = FirstUnhealthyStatus(
            usedSupplementalStatuses);
        return supplementalFailure is ThreadRepositoryStatus.Healthy
            ? DataSourceHealthStatus.Healthy
            : DataSourceHealthStatus.Degraded(ThreadRepositoryDetail(supplementalFailure));
    }

    private static string ThreadRepositoryDetail(ThreadRepositoryStatus status) => status switch
    {
        ThreadRepositoryStatus.Missing => "未找到 Codex 任务数据库",
        ThreadRepositoryStatus.Busy => "Codex 任务数据库正忙",
        ThreadRepositoryStatus.Incompatible => "Codex 任务数据库格式暂不兼容",
        ThreadRepositoryStatus.Unavailable => "Codex 任务数据库暂不可用",
        _ => "Codex 任务数据库正常",
    };

    private static DataSourceHealthStatus RenameHealth(SessionIndexStatus status) => status switch
    {
        SessionIndexStatus.Healthy => DataSourceHealthStatus.Healthy,
        SessionIndexStatus.Missing => DataSourceHealthStatus.Unavailable("未找到 Rename 索引"),
        SessionIndexStatus.Incompatible =>
            DataSourceHealthStatus.Unavailable("Rename 索引格式暂不兼容"),
        _ => DataSourceHealthStatus.Unavailable("Rename 索引暂不可用"),
    };

    private static DataSourceHealthStatus RolloutHealth(int successCount, int failureCount)
    {
        if (successCount == 0 && failureCount == 0)
        {
            return DataSourceHealthStatus.NotUsed;
        }

        if (failureCount == 0)
        {
            return DataSourceHealthStatus.Healthy;
        }

        return successCount == 0
            ? DataSourceHealthStatus.Unavailable("Rollout 数据不可用")
            : DataSourceHealthStatus.Degraded("部分 Rollout 无法读取");
    }

    private static DataSourceHealthStatus ServiceLogHealth(
        ServiceLogSourceStatus status) => status switch
    {
        ServiceLogSourceStatus.Healthy => DataSourceHealthStatus.Healthy,
        ServiceLogSourceStatus.NotUsed => DataSourceHealthStatus.NotUsed,
        ServiceLogSourceStatus.Missing =>
            DataSourceHealthStatus.Unavailable("未找到服务日志数据库"),
        ServiceLogSourceStatus.Busy => DataSourceHealthStatus.Degraded("服务日志数据库正忙"),
        ServiceLogSourceStatus.Incompatible =>
            DataSourceHealthStatus.Unavailable("服务日志数据库格式暂不兼容"),
        _ => DataSourceHealthStatus.Unavailable("服务日志数据库暂不可用"),
    };

    private static ServiceLogLoadResult EmptyServiceLogResult(ServiceLogSourceStatus status) =>
        new(status, new Dictionary<string, ServiceIncident>(StringComparer.Ordinal));

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

    private static ThreadRepositoryStatus FirstUnhealthyStatus(
        params ThreadRepositoryStatus[] statuses) =>
        statuses.FirstOrDefault(status => status is not ThreadRepositoryStatus.Healthy);

    private SubagentSnapshot CreateSubagentSnapshot(
        SubagentRecord record,
        IReadOnlyDictionary<string, string> titleOverrides,
        DateTimeOffset now,
        RolloutLoadResult? cachedRollout = null)
    {
        RolloutLoadResult rollout = cachedRollout ?? rolloutParser.Parse(record.RolloutPath);
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
            rollout.Status,
            record.AgentPath);
    }

    private static SubagentActivityLoadResult EmptySubagentActivityResult(
        ThreadRepositoryStatus status) =>
        new(
            status,
            new Dictionary<string, IReadOnlyList<SubagentActivityCandidate>>(StringComparer.Ordinal));
}
