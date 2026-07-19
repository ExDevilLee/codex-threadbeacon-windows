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

        IOrderedEnumerable<ThreadSnapshot> sorted = candidates
            .OrderBy(snapshot => ThreadStatusPriority.Get(snapshot.Status))
            .ThenByDescending(snapshot => updated.PinnedThreadIds.Contains(snapshot.Id))
            .ThenByDescending(snapshot => snapshot.LatestEventAt ?? DateTimeOffset.MinValue)
            .ThenBy(snapshot => snapshot.Id, StringComparer.Ordinal);

        var visible = new List<ThreadSnapshot>(Math.Min(candidates.Count, limit));
        var ignored = new List<ThreadSnapshot>();
        foreach (ThreadSnapshot snapshot in sorted)
        {
            if (updated.IgnoredRules.ContainsKey(snapshot.Id))
            {
                ignored.Add(snapshot);
            }
            else if (visible.Count < limit)
            {
                visible.Add(snapshot);
            }
        }

        return new ThreadListResult(visible, ignored, updated);
    }
}
