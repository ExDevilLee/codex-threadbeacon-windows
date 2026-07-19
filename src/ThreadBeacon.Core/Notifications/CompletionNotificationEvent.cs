namespace ThreadBeacon.Core.Notifications;

public sealed record CompletionNotificationEvent(
    string EventId,
    string ThreadId,
    DateTimeOffset OccurredAt);
