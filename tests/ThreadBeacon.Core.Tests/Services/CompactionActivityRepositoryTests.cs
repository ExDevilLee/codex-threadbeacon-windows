using System.Text.Json;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class CompactionActivityRepositoryTests
{
    [Fact]
    public void WritePreCompactAndReadReturnsActivity()
    {
        using var fixture = new ActivityFixture();
        var startedAt = DateTimeOffset.UtcNow.AddSeconds(-2);
        var activity = new CompactionActivity("session-a", "turn-a", "auto", startedAt);

        fixture.Repository.WritePreCompact(activity);

        Assert.Equal(activity, fixture.Repository.Read("session-a", null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void PostCompactDeletesOnlyMatchingTurn()
    {
        using var fixture = new ActivityFixture();
        var activity = new CompactionActivity("session-a", "turn-a", "manual", DateTimeOffset.UtcNow.AddSeconds(-2));
        fixture.Repository.WritePreCompact(activity);

        fixture.Repository.ClearPostCompact(new CompactionActivity("session-a", "turn-b", "manual", activity.StartedAt));
        Assert.NotNull(fixture.Repository.Read("session-a", null, null, DateTimeOffset.UtcNow));

        fixture.Repository.ClearPostCompact(activity);
        Assert.Null(fixture.Repository.Read("session-a", null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ReadRejectsExpiredFutureAndStaleLifecycleEvidence()
    {
        using var fixture = new ActivityFixture();
        var now = DateTimeOffset.UtcNow;
        fixture.Repository.WritePreCompact(new CompactionActivity("session-a", "turn-a", "auto", now.AddMinutes(-16)));
        Assert.Null(fixture.Repository.Read("session-a", null, null, now));

        fixture.Repository.WritePreCompact(new CompactionActivity("session-a", "turn-a", "auto", now.AddMinutes(1)));
        Assert.Null(fixture.Repository.Read("session-a", null, null, now));

        fixture.Repository.WritePreCompact(new CompactionActivity("session-a", "turn-a", "auto", now.AddSeconds(-2)));
        Assert.Null(fixture.Repository.Read("session-a", now, null, now));
        Assert.Null(fixture.Repository.Read("session-a", null, now, now));
    }

    [Fact]
    public void ReadIsolatesSessionsAndIgnoresMalformedMarkers()
    {
        using var fixture = new ActivityFixture();
        fixture.Repository.WritePreCompact(new CompactionActivity("session-a", "turn-a", "auto", DateTimeOffset.UtcNow));
        File.WriteAllText(
            Path.Combine(fixture.ActiveDirectory, "session-b.json"),
            "{\"schemaVersion\":99,\"sessionId\":\"session-b\"}");

        Assert.NotNull(fixture.Repository.Read("session-a", null, null, DateTimeOffset.UtcNow));
        Assert.Null(fixture.Repository.Read("session-b", null, null, DateTimeOffset.UtcNow));
    }

    private sealed class ActivityFixture : IDisposable
    {
        public ActivityFixture()
        {
            ActiveDirectory = Path.Combine(Path.GetTempPath(), "ThreadBeaconActivity", Guid.NewGuid().ToString("N"));
            Repository = new CompactionActivityRepository(ActiveDirectory);
        }

        public string ActiveDirectory { get; }

        public CompactionActivityRepository Repository { get; }

        public void Dispose()
        {
            if (Directory.Exists(ActiveDirectory))
            {
                Directory.Delete(ActiveDirectory, recursive: true);
            }
        }
    }
}
