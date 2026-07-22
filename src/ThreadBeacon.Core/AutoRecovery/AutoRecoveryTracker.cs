using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;

namespace ThreadBeacon.Core.AutoRecovery;

public sealed record AutoRecoveryCandidate(
    string ThreadId,
    string EpisodeId,
    AutoRecoveryIncidentType IncidentType,
    string Title,
    string RolloutPath,
    DateTimeOffset OccurredAt);

public sealed class AutoRecoveryTracker
{
    public const int MaximumHistory = 256;

    private readonly List<string> seenEpisodeIds = [];
    private readonly HashSet<string> seenEpisodeIdSet = new(StringComparer.Ordinal);

    public IReadOnlyList<string> SeenEpisodeIds => seenEpisodeIds;

    public IReadOnlyList<AutoRecoveryCandidate> Observe(
        IReadOnlyList<ThreadSnapshot> snapshots,
        RefreshNotificationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        var candidates = new List<AutoRecoveryCandidate>();
        foreach (ThreadSnapshot snapshot in snapshots)
        {
            if (CreateCandidate(snapshot) is not AutoRecoveryCandidate candidate)
            {
                continue;
            }

            string eventId = $"{candidate.ThreadId}:{candidate.EpisodeId}";
            if (!seenEpisodeIdSet.Add(eventId))
            {
                continue;
            }

            seenEpisodeIds.Add(eventId);
            if (policy is RefreshNotificationPolicy.Notify)
            {
                candidates.Add(candidate);
            }
        }

        TrimHistory();
        return candidates;
    }

    private static AutoRecoveryCandidate? CreateCandidate(ThreadSnapshot snapshot)
    {
        if (snapshot.IsArchived
            || string.IsNullOrWhiteSpace(snapshot.RolloutPath)
            || snapshot.ServiceIncident is not { Phase: ServiceIncidentPhase.Failed } incident)
        {
            return null;
        }

        AutoRecoveryIncidentType type = incident.Kind switch
        {
            ServiceIncidentKind.BadRequest => AutoRecoveryIncidentType.Http400,
            ServiceIncidentKind.HttpRateLimit => AutoRecoveryIncidentType.Http429,
            ServiceIncidentKind.ModelCapacity => AutoRecoveryIncidentType.ModelCapacity,
            ServiceIncidentKind.StreamDisconnected => AutoRecoveryIncidentType.StreamDisconnected,
            _ when incident.HttpStatusCode == 503 => AutoRecoveryIncidentType.Http503,
            _ => AutoRecoveryIncidentType.OtherHttp,
        };
        return new AutoRecoveryCandidate(
            snapshot.Id,
            incident.EpisodeId,
            type,
            snapshot.Title,
            snapshot.RolloutPath,
            incident.OccurredAt);
    }

    private void TrimHistory()
    {
        int removeCount = seenEpisodeIds.Count - MaximumHistory;
        for (int index = 0; index < removeCount; index++)
        {
            seenEpisodeIdSet.Remove(seenEpisodeIds[index]);
        }

        if (removeCount > 0)
        {
            seenEpisodeIds.RemoveRange(0, removeCount);
        }
    }
}
