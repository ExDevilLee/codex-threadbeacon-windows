using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.AutoRecovery;

public interface IAutoRecoveryCircuitStore
{
    IReadOnlyList<AutoRecoveryCircuitState> Load();

    AutoRecoveryCircuitState? StateFor(
        string threadId,
        AutoRecoveryIncidentType incidentType);

    AutoRecoveryCircuitState RecordAttempt(
        AutoRecoveryCandidate candidate,
        DateTimeOffset attemptedAt);

    void ObserveCompletion(string threadId, DateTimeOffset completedAt);

    void Reset(string threadId, AutoRecoveryIncidentType incidentType);
}
