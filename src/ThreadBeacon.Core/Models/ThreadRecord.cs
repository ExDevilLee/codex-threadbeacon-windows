namespace ThreadBeacon.Core.Models;

public sealed record ThreadRecord(
    string Id,
    string Title,
    string RolloutPath,
    DateTimeOffset UpdatedAt,
    long TokensUsed,
    int SubagentCount);
