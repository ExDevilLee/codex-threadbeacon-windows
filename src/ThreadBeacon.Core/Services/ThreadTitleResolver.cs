using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public static class ThreadTitleResolver
{
    public static IReadOnlyList<ThreadRecord> Resolve(
        IEnumerable<ThreadRecord> records,
        IReadOnlyDictionary<string, string> titleOverrides)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(titleOverrides);

        return records
            .Select(record => titleOverrides.TryGetValue(record.Id, out string? title)
                && !string.IsNullOrWhiteSpace(title)
                ? record with { Title = title.Trim() }
                : record)
            .ToArray();
    }
}
