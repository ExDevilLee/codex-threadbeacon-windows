namespace ThreadBeacon.Core.Models;

public sealed record SubagentSnapshot(
    string Id,
    string Title,
    ThreadStatus Status,
    DateTimeOffset StatusChangedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LatestEventAt,
    TokenUsageSnapshot? TokenUsage,
    string? AgentNickname,
    string? AgentRole,
    string? Model,
    string? ReasoningEffort,
    RolloutSourceStatus RolloutSourceStatus);
