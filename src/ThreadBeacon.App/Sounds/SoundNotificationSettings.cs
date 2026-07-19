namespace ThreadBeacon.App.Sounds;

public sealed record SoundNotificationSettings
{
    public int Version { get; init; } = 1;

    public bool IsEnabled { get; init; } = true;

    public bool IsCompletionEnabled { get; init; } = true;

    public CompletionSound SelectedCompletionSound { get; init; } = CompletionSound.Beacon;

    public IReadOnlyList<string> SeenEventIds { get; init; } = [];
}
