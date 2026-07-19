namespace ThreadBeacon.Core.Models;

public sealed record ThreadSnapshotLoadResult
{
    public ThreadSnapshotLoadResult(
        ThreadRepositoryStatus threadSourceStatus,
        SessionIndexStatus titleSourceStatus,
        IReadOnlyList<ThreadSnapshot> threads,
        DateTimeOffset refreshedAt,
        DataSourceHealthReport health)
    {
        ThreadSourceStatus = threadSourceStatus;
        TitleSourceStatus = titleSourceStatus;
        Threads = threads ?? throw new ArgumentNullException(nameof(threads));
        RefreshedAt = refreshedAt;
        Health = health ?? throw new ArgumentNullException(nameof(health));
    }

    public ThreadRepositoryStatus ThreadSourceStatus { get; }
    public SessionIndexStatus TitleSourceStatus { get; }
    public IReadOnlyList<ThreadSnapshot> Threads { get; }
    public DateTimeOffset RefreshedAt { get; }
    public DataSourceHealthReport Health { get; }
    public bool IsHealthy => Health.OverallStatus is OverallDataSourceHealth.Healthy;
}
