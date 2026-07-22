namespace ThreadBeacon.Core.Models;

public enum ServiceIncidentPhase
{
    Retrying,
    Failed,
}

public enum ServiceIncidentKind
{
    BadRequest,
    ServiceUnavailable,
    HttpRateLimit,
    ModelCapacity,
    OtherHttp,
    StreamDisconnected,
}

public sealed record ServiceIncident(
    string EpisodeId,
    ServiceIncidentPhase Phase,
    int? HttpStatusCode,
    int? RetryAttempt,
    int? RetryLimit,
    DateTimeOffset OccurredAt,
    ServiceIncidentKind Kind = ServiceIncidentKind.ServiceUnavailable);
