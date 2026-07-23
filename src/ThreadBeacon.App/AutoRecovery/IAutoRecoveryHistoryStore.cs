using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.AutoRecovery;

public enum AutoRecoveryHistoryStatus
{
    NotSent,
    Sending,
    Sent,
    Failed,
    CircuitOpen,
}

public sealed record AutoRecoveryHistoryEntry(
    string AttemptId,
    string ThreadId,
    string EpisodeId,
    AutoRecoveryIncidentType IncidentType,
    AutoRecoveryHistoryStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? DiagnosticCode = null);

public interface IAutoRecoveryHistoryStore
{
    IReadOnlyList<AutoRecoveryHistoryEntry> Load();

    bool Upsert(AutoRecoveryHistoryEntry entry);
}
