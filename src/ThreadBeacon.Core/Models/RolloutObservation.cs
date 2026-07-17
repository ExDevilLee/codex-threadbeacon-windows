namespace ThreadBeacon.Core.Models;

public sealed record RolloutObservation(
    ThreadStatus Status,
    DateTimeOffset? StatusChangedAt,
    DateTimeOffset? LatestEventAt,
    DateTimeOffset? CompletionEventAt,
    DateTimeOffset? LatestTaskStartedAt,
    TokenUsageSnapshot? TokenUsage)
{
    public static RolloutObservation Empty { get; } = new(
        ThreadStatus.Unknown,
        null,
        null,
        null,
        null,
        null);
}
