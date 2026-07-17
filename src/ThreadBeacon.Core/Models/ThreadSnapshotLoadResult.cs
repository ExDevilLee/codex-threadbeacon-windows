namespace ThreadBeacon.Core.Models;

public sealed record ThreadSnapshotLoadResult(
    ThreadRepositoryStatus ThreadSourceStatus,
    SessionIndexStatus TitleSourceStatus,
    IReadOnlyList<ThreadSnapshot> Threads,
    DateTimeOffset RefreshedAt)
{
    public bool IsHealthy =>
        ThreadSourceStatus is ThreadRepositoryStatus.Healthy
        && TitleSourceStatus is SessionIndexStatus.Healthy
        && Threads.All(thread => thread.RolloutSourceStatus is RolloutSourceStatus.Healthy);
}
