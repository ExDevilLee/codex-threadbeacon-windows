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
            if (snapshot.CompletionEventAt is not DateTimeOffset occurredAt)
            {
                continue;
            }

            string eventId = CreateEventId(snapshot.Id, occurredAt);
            if (!seenEventIdSet.Add(eventId))
            {
                continue;
            }

            seenEventIds.Add(eventId);
            firstNewEvent ??= new CompletionNotificationEvent(
                eventId,
                snapshot.Id,
                occurredAt);
        }

        TrimHistory();
        return policy is RefreshNotificationPolicy.Notify ? firstNewEvent : null;
    }

    private static string CreateEventId(string threadId, DateTimeOffset occurredAt) =>
        $"done:{threadId}:{occurredAt.ToUnixTimeMilliseconds()}";

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
