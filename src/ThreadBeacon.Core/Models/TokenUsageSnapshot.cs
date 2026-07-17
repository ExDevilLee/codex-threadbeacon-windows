namespace ThreadBeacon.Core.Models;

public sealed record TokenUsageSnapshot(
    long TotalTokens,
    TokenUsage? Cumulative,
    TokenUsage? CurrentTurn,
    DateTimeOffset? UpdatedAt);
