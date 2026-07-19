using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Notifications;

public sealed class CompletionNotificationTracker
{
    public const int MaximumHistory = 256;

    private readonly List<string> seenEventIds = [];
    private readonly HashSet<string> seenEventIdSet = new(StringComparer.Ordinal);

    public CompletionNotificationTracker(IEnumerable<string>? seenEventIds = null)
    {
        if (seenEventIds is null)
        {
            return;
        }

        foreach (string eventId in seenEventIds)
        {
            if (!string.IsNullOrWhiteSpace(eventId) && seenEventIdSet.Add(eventId))
            {
                this.seenEventIds.Add(eventId);
            }
        }

        TrimHistory();
    }

    public IReadOnlyList<string> SeenEventIds => seenEventIds;

    public CompletionNotificationEvent? Observe(
        IReadOnlyList<ThreadSnapshot> snapshots,
        RefreshNotificationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        CompletionNotificationEvent? firstNewEvent = null;
        foreach (ThreadSnapshot snapshot in snapshots)
        {
            CompletionNotificationEvent? candidate = CreateCandidate(snapshot);
            if (candidate is null)
            {
                continue;
            }

            if (!seenEventIdSet.Add(candidate.EventId))
            {
                continue;
            }

            seenEventIds.Add(candidate.EventId);
            if (firstNewEvent is null
                || candidate.Category is SoundNotificationCategory.Warning
                    && firstNewEvent.Category is SoundNotificationCategory.Done)
            {
                firstNewEvent = candidate;
            }
        }

        TrimHistory();
        return policy is RefreshNotificationPolicy.Notify ? firstNewEvent : null;
    }

    private static CompletionNotificationEvent? CreateCandidate(ThreadSnapshot snapshot)
    {
        if (snapshot.IsArchived)
        {
            return null;
        }

        if (snapshot.ServiceIncident is ServiceIncident incident)
        {
            return new CompletionNotificationEvent(
                $"warning:{snapshot.Id}:{incident.EpisodeId}",
                snapshot.Id,
                incident.OccurredAt,
                SoundNotificationCategory.Warning);
        }

        return snapshot.CompletionEventAt is DateTimeOffset completedAt
            ? new CompletionNotificationEvent(
                $"done:{snapshot.Id}:{completedAt.ToUnixTimeMilliseconds()}",
                snapshot.Id,
                completedAt)
            : null;
    }

    private void TrimHistory()
    {
        int removeCount = seenEventIds.Count - MaximumHistory;
        if (removeCount <= 0)
        {
            return;
        }

        for (int index = 0; index < removeCount; index++)
        {
            seenEventIdSet.Remove(seenEventIds[index]);
        }

        seenEventIds.RemoveRange(0, removeCount);
    }
}
