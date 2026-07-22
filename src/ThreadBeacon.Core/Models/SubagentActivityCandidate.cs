namespace ThreadBeacon.Core.Models;

public sealed record SubagentActivityCandidate(
    string Id,
    string ParentId,
    string RolloutPath,
    DateTimeOffset UpdatedAt);
