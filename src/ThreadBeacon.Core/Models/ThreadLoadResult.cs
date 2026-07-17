namespace ThreadBeacon.Core.Models;

public sealed record ThreadLoadResult(
    ThreadRepositoryStatus Status,
    IReadOnlyList<ThreadRecord> Threads)
{
    public bool IsHealthy => Status is ThreadRepositoryStatus.Healthy;
}
