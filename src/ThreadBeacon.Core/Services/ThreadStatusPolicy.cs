using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public static class ThreadStatusPolicy
{
    public static ThreadDisplayState Evaluate(
        RolloutObservation observation,
        DateTimeOffset fallbackDate,
        DateTimeOffset now,
        TimeSpan? completedRetention = null,
        TimeSpan? runningFreshness = null)
    {
        ArgumentNullException.ThrowIfNull(observation);
        TimeSpan retention = completedRetention ?? TimeSpan.FromSeconds(60);
        TimeSpan freshness = runningFreshness ?? TimeSpan.FromSeconds(120);
        if (retention < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(completedRetention));
        }

        if (freshness < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(runningFreshness));
        }

        if (observation.Status is ThreadStatus.JustCompleted
            && observation.StatusChangedAt is { } completedAt
            && now - completedAt > retention)
        {
            return new ThreadDisplayState(ThreadStatus.Idle, completedAt);
        }

        if (observation.Status is ThreadStatus.Running
            && observation.LatestEventAt is { } latestEventAt
            && now - latestEventAt > freshness)
        {
            return new ThreadDisplayState(ThreadStatus.Unknown, latestEventAt);
        }

        return new ThreadDisplayState(
            observation.Status,
            observation.StatusChangedAt ?? fallbackDate);
    }
}
