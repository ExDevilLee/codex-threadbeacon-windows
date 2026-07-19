namespace ThreadBeacon.Core.Models;

public enum ThreadIgnoreMode
{
    UntilNextTurn,
}

public sealed record IgnoredThreadRule(
    string ThreadId,
    DateTimeOffset IgnoredAt,
    ThreadIgnoreMode Mode);

public sealed class ThreadListPreferences
{
    public ThreadListPreferences(
        IEnumerable<string>? pinnedThreadIds = null,
        IReadOnlyDictionary<string, IgnoredThreadRule>? ignoredRules = null)
    {
        PinnedThreadIds = new HashSet<string>(
            pinnedThreadIds ?? [],
            StringComparer.Ordinal);
        IgnoredRules = ignoredRules is null
            ? new Dictionary<string, IgnoredThreadRule>(StringComparer.Ordinal)
            : new Dictionary<string, IgnoredThreadRule>(ignoredRules, StringComparer.Ordinal);
    }

    public HashSet<string> PinnedThreadIds { get; }

    public Dictionary<string, IgnoredThreadRule> IgnoredRules { get; }

    public ThreadListPreferences Clone() => new(PinnedThreadIds, IgnoredRules);
}
