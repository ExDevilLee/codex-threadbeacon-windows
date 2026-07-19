namespace ThreadBeacon.Core.Notifications;

public enum SoundNotificationCategory
{
    Done,
    Warning,
}

public sealed record CompletionNotificationEvent(
    string EventId,
    string ThreadId,
    DateTimeOffset OccurredAt,
    SoundNotificationCategory Category = SoundNotificationCategory.Done);
