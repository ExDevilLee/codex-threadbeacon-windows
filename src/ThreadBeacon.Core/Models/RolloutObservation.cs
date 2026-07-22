namespace ThreadBeacon.Core.Models;

public sealed record RolloutObservation(
    ThreadStatus Status,
    DateTimeOffset? StatusChangedAt,
    DateTimeOffset? LatestEventAt,
    DateTimeOffset? CompletionEventAt,
    DateTimeOffset? LatestTaskStartedAt,
    TokenUsageSnapshot? TokenUsage,
    string? Model = null,
    string? ReasoningEffort = null)
{
    public static RolloutObservation Empty { get; } = new(
        ThreadStatus.Unknown,
        null,
        null,
        null,
        null,
        null);
}
