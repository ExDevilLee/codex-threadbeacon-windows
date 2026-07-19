namespace ThreadBeacon.Core.Models;

public sealed record ThreadLoadRequest(
    int RecentLimit,
    IReadOnlySet<string> IncludedThreadIds,
    IReadOnlySet<string> ExpandedThreadIds,
    IReadOnlySet<string>? FavoriteThreadIds = null);
