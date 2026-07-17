namespace ThreadBeacon.Core.Models;

public sealed record ThreadSnapshot(
    string Id,
    string Title,
    ThreadStatus Status,
    DateTimeOffset StatusChangedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LatestEventAt,
    DateTimeOffset? LatestTaskStartedAt,
    DateTimeOffset? CompletionEventAt,
    TokenUsageSnapshot? TokenUsage,
    int SubagentCount,
    RolloutSourceStatus RolloutSourceStatus);
