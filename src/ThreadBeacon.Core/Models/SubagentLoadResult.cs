namespace ThreadBeacon.Core.Models;

public sealed record SubagentLoadResult(
    ThreadRepositoryStatus Status,
    IReadOnlyDictionary<string, IReadOnlyList<SubagentRecord>> SubagentsByParent);
