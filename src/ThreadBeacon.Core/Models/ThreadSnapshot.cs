namespace ThreadBeacon.Core.Models;

public sealed record ThreadSnapshot
{
    public ThreadSnapshot(
        string id,
        string title,
        ThreadStatus status,
        DateTimeOffset statusChangedAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? latestEventAt,
        DateTimeOffset? latestTaskStartedAt,
        DateTimeOffset? completionEventAt,
        TokenUsageSnapshot? tokenUsage,
        int subagentCount,
        RolloutSourceStatus rolloutSourceStatus,
        IReadOnlyList<SubagentSnapshot>? subagents = null,
        ThreadRepositoryStatus subagentSourceStatus = ThreadRepositoryStatus.Healthy,
        ServiceIncident? serviceIncident = null)
    {
        Id = id;
        Title = title;
        Status = status;
        StatusChangedAt = statusChangedAt;
        UpdatedAt = updatedAt;
        LatestEventAt = latestEventAt;
        LatestTaskStartedAt = latestTaskStartedAt;
        CompletionEventAt = completionEventAt;
        TokenUsage = tokenUsage;
        SubagentCount = Math.Max(0, subagentCount);
        RolloutSourceStatus = rolloutSourceStatus;
        Subagents = subagents ?? Array.Empty<SubagentSnapshot>();
        SubagentSourceStatus = subagentSourceStatus;
        ServiceIncident = serviceIncident;
    }

    public string Id { get; }
    public string Title { get; }
    public ThreadStatus Status { get; }
    public DateTimeOffset StatusChangedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
    public DateTimeOffset? LatestEventAt { get; }
    public DateTimeOffset? LatestTaskStartedAt { get; }
    public DateTimeOffset? CompletionEventAt { get; }
    public TokenUsageSnapshot? TokenUsage { get; }
    public int SubagentCount { get; }
    public RolloutSourceStatus RolloutSourceStatus { get; }
    public IReadOnlyList<SubagentSnapshot> Subagents { get; }
    public ThreadRepositoryStatus SubagentSourceStatus { get; }
    public ServiceIncident? ServiceIncident { get; }
}
