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
        IEnumerable<string>? favoriteThreadIds = null,
        bool showsFavoritesOnly = false,
        IReadOnlyDictionary<string, IgnoredThreadRule>? ignoredRules = null)
    {
        PinnedThreadIds = new HashSet<string>(
            pinnedThreadIds ?? [],
            StringComparer.Ordinal);
        FavoriteThreadIds = new HashSet<string>(
            favoriteThreadIds ?? [],
            StringComparer.Ordinal);
        ShowsFavoritesOnly = showsFavoritesOnly;
        IgnoredRules = ignoredRules is null
            ? new Dictionary<string, IgnoredThreadRule>(StringComparer.Ordinal)
            : new Dictionary<string, IgnoredThreadRule>(ignoredRules, StringComparer.Ordinal);
    }

    public HashSet<string> PinnedThreadIds { get; }

    public HashSet<string> FavoriteThreadIds { get; }

    public bool ShowsFavoritesOnly { get; set; }

    public Dictionary<string, IgnoredThreadRule> IgnoredRules { get; }

    public ThreadListPreferences Clone() =>
        new(PinnedThreadIds, FavoriteThreadIds, ShowsFavoritesOnly, IgnoredRules);
}
