namespace ThreadBeacon.Core.Models;

public sealed record ThreadListResult(
    IReadOnlyList<ThreadSnapshot> VisibleSnapshots,
    IReadOnlyList<ThreadSnapshot> IgnoredSnapshots,
    ThreadListPreferences Preferences);
