using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class ThreadTitleResolverTests
{
    [Fact]
    public void Resolve_PrefersRenameAndFallsBackToSqliteTitle()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ThreadRecord[] records =
        [
            new("thread-a", "Original prompt", "D:\\a.jsonl", now, 1, 0),
            new("thread-b", "SQLite fallback", "D:\\b.jsonl", now, 2, 0),
        ];
        var titles = new Dictionary<string, string>
        {
            ["thread-a"] = " Renamed task ",
            ["thread-b"] = "  ",
        };

        IReadOnlyList<ThreadRecord> resolved = ThreadTitleResolver.Resolve(records, titles);

        Assert.Equal("Renamed task", resolved[0].Title);
        Assert.Equal("SQLite fallback", resolved[1].Title);
        Assert.Equal("Original prompt", records[0].Title);
    }
}
