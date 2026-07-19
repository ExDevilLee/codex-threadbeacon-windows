using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public static class ThreadListPolicy
{
    public static ThreadListResult Evaluate(
        IReadOnlyList<ThreadSnapshot> candidates,
        ThreadListPreferences preferences,
        int limit)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentOutOfRangeException.ThrowIfNegative(limit);

        ThreadListPreferences updated = preferences.Clone();
        foreach (ThreadSnapshot snapshot in candidates)
        {
            if (updated.IgnoredRules.TryGetValue(snapshot.Id, out IgnoredThreadRule? rule)
                && rule.Mode is ThreadIgnoreMode.UntilNextTurn
                && snapshot.LatestTaskStartedAt > rule.IgnoredAt)
            {
                updated.IgnoredRules.Remove(snapshot.Id);
            }
        }

        IEnumerable<ThreadSnapshot> displayCandidates = updated.ShowsFavoritesOnly
            ? candidates.Where(snapshot => updated.FavoriteThreadIds.Contains(snapshot.Id))
            : candidates;
        IOrderedEnumerable<ThreadSnapshot> visibleCandidates = Order(
            displayCandidates.Where(snapshot => !updated.IgnoredRules.ContainsKey(snapshot.Id)),
            updated.PinnedThreadIds);
        IOrderedEnumerable<ThreadSnapshot> ignoredCandidates = Order(
            candidates.Where(snapshot => updated.IgnoredRules.ContainsKey(snapshot.Id)),
            updated.PinnedThreadIds);

        return new ThreadListResult(
            visibleCandidates.Take(limit).ToArray(),
            ignoredCandidates.ToArray(),
            updated);
    }

    private static IOrderedEnumerable<ThreadSnapshot> Order(
        IEnumerable<ThreadSnapshot> snapshots,
        IReadOnlySet<string> pinnedThreadIds) =>
        snapshots
            .OrderBy(snapshot => ThreadStatusPriority.Get(snapshot.Status))
            .ThenByDescending(snapshot => pinnedThreadIds.Contains(snapshot.Id))
            .ThenByDescending(snapshot => snapshot.LatestEventAt ?? DateTimeOffset.MinValue)
            .ThenBy(snapshot => snapshot.Id, StringComparer.Ordinal);
}
