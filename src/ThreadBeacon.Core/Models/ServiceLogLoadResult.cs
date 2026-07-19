namespace ThreadBeacon.Core.Models;

public enum ServiceLogSourceStatus
{
    Healthy,
    Missing,
    Busy,
    Incompatible,
    Unavailable,
    NotUsed,
}

public sealed record ServiceLogLoadResult(
    ServiceLogSourceStatus Status,
    IReadOnlyDictionary<string, ServiceIncident> Incidents);
