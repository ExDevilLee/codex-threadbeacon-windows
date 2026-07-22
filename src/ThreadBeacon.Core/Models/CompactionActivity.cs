namespace ThreadBeacon.Core.Models;

public sealed record CompactionActivity(
    string SessionId,
    string TurnId,
    string Trigger,
    DateTimeOffset StartedAt);
