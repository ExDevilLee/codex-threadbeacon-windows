namespace ThreadBeacon.App.Settings;

public sealed record AppSettings
{
    public int Version { get; init; } = 1;

    public bool IsWindowPinned { get; init; }
}
