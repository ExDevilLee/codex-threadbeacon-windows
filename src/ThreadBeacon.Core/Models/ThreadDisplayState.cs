namespace ThreadBeacon.Core.Models;

public sealed record ThreadDisplayState(
    ThreadStatus Status,
    DateTimeOffset ChangedAt);
