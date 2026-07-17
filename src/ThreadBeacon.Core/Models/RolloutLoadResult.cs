namespace ThreadBeacon.Core.Models;

public sealed record RolloutLoadResult(
    RolloutSourceStatus Status,
    RolloutObservation Observation)
{
    public bool IsHealthy => Status is RolloutSourceStatus.Healthy;
}
