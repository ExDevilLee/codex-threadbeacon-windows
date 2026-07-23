namespace ThreadBeacon.Core.AutoRecovery;

public sealed record AutoRecoveryCircuitState(
    string ThreadId,
    AutoRecoveryIncidentType IncidentType,
    int AttemptCount,
    string LastEpisodeId,
    DateTimeOffset LastAttemptAt)
{
    public string Id => IdFor(ThreadId, IncidentType);

    public static string IdFor(
        string threadId,
        AutoRecoveryIncidentType incidentType) => $"{threadId}:{incidentType}";
}
