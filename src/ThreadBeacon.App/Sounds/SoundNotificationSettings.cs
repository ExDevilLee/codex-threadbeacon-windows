namespace ThreadBeacon.App.Sounds;

public sealed record SoundNotificationSettings
{
    public int Version { get; init; } = 1;

    public bool IsEnabled { get; init; } = true;

    public bool IsCompletionEnabled { get; init; } = true;

    public CompletionSound SelectedCompletionSound { get; init; } = CompletionSound.Chime;

    public string? CompletionSoundPath { get; init; }

    public bool IsWarningEnabled { get; init; } = true;

    public CompletionSound SelectedWarningSound { get; init; } = CompletionSound.Alert;

    public string? WarningSoundPath { get; init; }

    public IReadOnlyList<string> SeenEventIds { get; init; } = [];
}
