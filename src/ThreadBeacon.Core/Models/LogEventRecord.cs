namespace ThreadBeacon.Core.Models;

public sealed record LogEventRecord(
    string ThreadId,
    DateTimeOffset OccurredAt,
    string Target,
    string Body);
