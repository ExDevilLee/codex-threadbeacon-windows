namespace ThreadBeacon.Core.Models;

public sealed record SubagentActivityLoadResult(
    ThreadRepositoryStatus Status,
    IReadOnlyDictionary<string, IReadOnlyList<SubagentActivityCandidate>> CandidatesByParent);
