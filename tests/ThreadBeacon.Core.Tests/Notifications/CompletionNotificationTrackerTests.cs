using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;

namespace ThreadBeacon.Core.Tests.Notifications;

public sealed class CompletionNotificationTrackerTests
{
    [Fact]
    public void Observe_BaselineRecordsCompletionWithoutReturningNotification()
    {
        var tracker = new CompletionNotificationTracker();

        CompletionNotificationEvent? result = tracker.Observe(
            [Completed("thread-1", AtSeconds(10))],
            RefreshNotificationPolicy.Baseline);

        Assert.Null(result);
        Assert.Equal(["done:thread-1:10000"], tracker.SeenEventIds);
    }

    [Fact]
    public void Observe_NotifyReturnsNewCompletionOnlyOnce()
    {
        var tracker = new CompletionNotificationTracker();
        ThreadSnapshot snapshot = Completed("thread-1", AtSeconds(10));

        CompletionNotificationEvent? first = tracker.Observe(
            [snapshot],
            RefreshNotificationPolicy.Notify);
        CompletionNotificationEvent? second = tracker.Observe(
            [snapshot],
            RefreshNotificationPolicy.Notify);

        Assert.NotNull(first);
        Assert.Equal("done:thread-1:10000", first.EventId);
        Assert.Equal("thread-1", first.ThreadId);
        Assert.Equal(AtSeconds(10), first.OccurredAt);
        Assert.Null(second);
    }

    [Fact]
    public void Observe_RecordsBatchButReturnsOnlyOneNotification()
    {
        var tracker = new CompletionNotificationTracker();

        CompletionNotificationEvent? result = tracker.Observe(
            [
                Completed("thread-1", AtSeconds(10)),
                Completed("thread-2", AtSeconds(20)),
            ],
            RefreshNotificationPolicy.Notify);

        Assert.Equal("done:thread-1:10000", result?.EventId);
        Assert.Equal(
            ["done:thread-1:10000", "done:thread-2:20000"],
            tracker.SeenEventIds);
    }

    [Fact]
    public void Observe_IgnoresSnapshotsWithoutCompletionEvidence()
    {
        var tracker = new CompletionNotificationTracker();

        CompletionNotificationEvent? result = tracker.Observe(
            [Snapshot("thread-1", null)],
            RefreshNotificationPolicy.Notify);

        Assert.Null(result);
        Assert.Empty(tracker.SeenEventIds);
    }

    [Fact]
    public void Observe_SuppliedHistorySuppressesExistingCompletion()
    {
        var tracker = new CompletionNotificationTracker(["done:thread-1:10000"]);

        CompletionNotificationEvent? result = tracker.Observe(
            [Completed("thread-1", AtSeconds(10))],
            RefreshNotificationPolicy.Notify);

        Assert.Null(result);
        Assert.Equal(["done:thread-1:10000"], tracker.SeenEventIds);
    }

    [Fact]
    public void Observe_KeepsOnlyNewestBoundedHistory()
    {
        var tracker = new CompletionNotificationTracker();

        for (int index = 0; index <= CompletionNotificationTracker.MaximumHistory; index++)
        {
            tracker.Observe(
                [Completed($"thread-{index}", AtSeconds(index))],
                RefreshNotificationPolicy.Baseline);
        }

        Assert.Equal(CompletionNotificationTracker.MaximumHistory, tracker.SeenEventIds.Count);
        Assert.DoesNotContain("done:thread-0:0", tracker.SeenEventIds);
        Assert.Equal("done:thread-1:1000", tracker.SeenEventIds[0]);
        Assert.Equal("done:thread-256:256000", tracker.SeenEventIds[^1]);
    }

    [Fact]
    public void Observe_EmitsWarningOnlyOnceWhenEpisodeBecomesFailure()
    {
        var tracker = new CompletionNotificationTracker();
        ThreadSnapshot retrying = Incident(
            "thread-1",
            "turn-a",
            ServiceIncidentPhase.Retrying,
            AtSeconds(20));
        ThreadSnapshot failed = Incident(
            "thread-1",
            "turn-a",
            ServiceIncidentPhase.Failed,
            AtSeconds(21));

        var first = tracker.Observe([retrying], RefreshNotificationPolicy.Notify);
        var repeated = tracker.Observe([failed], RefreshNotificationPolicy.Notify);

        Assert.Equal(SoundNotificationCategory.Warning, first?.Category);
        Assert.Equal("warning:thread-1:turn-a", first?.EventId);
        Assert.Null(repeated);
        Assert.Equal(["warning:thread-1:turn-a"], tracker.SeenEventIds);
    }

    [Fact]
    public void Observe_IncidentTakesPriorityOverCompletionEvidence()
    {
        var tracker = new CompletionNotificationTracker();
        DateTimeOffset occurredAt = AtSeconds(30);
        ThreadSnapshot snapshot = new(
            "thread-1",
            "thread-1",
            ThreadStatus.Error,
            occurredAt,
            occurredAt,
            occurredAt,
            null,
            occurredAt,
            null,
            0,
            RolloutSourceStatus.Healthy,
            serviceIncident: new ServiceIncident(
                "turn-failed",
                ServiceIncidentPhase.Failed,
                503,
                5,
                5,
                occurredAt));

        var result = tracker.Observe([snapshot], RefreshNotificationPolicy.Notify);

        Assert.Equal(SoundNotificationCategory.Warning, result?.Category);
        Assert.Equal(["warning:thread-1:turn-failed"], tracker.SeenEventIds);
    }

    [Fact]
    public void Observe_PrefersWarningAcrossSnapshotsWhileRecordingBoth()
    {
        var tracker = new CompletionNotificationTracker();

        var result = tracker.Observe(
            [
                Completed("completed", AtSeconds(10)),
                Incident("warning", "turn-warning", ServiceIncidentPhase.Retrying, AtSeconds(20)),
            ],
            RefreshNotificationPolicy.Notify);

        Assert.Equal(SoundNotificationCategory.Warning, result?.Category);
        Assert.Equal(
            ["done:completed:10000", "warning:warning:turn-warning"],
            tracker.SeenEventIds);
    }

    [Fact]
    public void Observe_IgnoresArchivedSnapshots()
    {
        var tracker = new CompletionNotificationTracker();
        DateTimeOffset occurredAt = AtSeconds(30);
        ThreadSnapshot archived = new(
            "archived",
            "archived",
            ThreadStatus.Error,
            occurredAt,
            occurredAt,
            occurredAt,
            null,
            occurredAt,
            null,
            0,
            RolloutSourceStatus.Healthy,
            serviceIncident: new ServiceIncident(
                "archived-turn",
                ServiceIncidentPhase.Failed,
                503,
                5,
                5,
                occurredAt),
            isArchived: true);

        CompletionNotificationEvent? result = tracker.Observe(
            [archived],
            RefreshNotificationPolicy.Notify);

        Assert.Null(result);
        Assert.Empty(tracker.SeenEventIds);
    }

    private static DateTimeOffset AtSeconds(long seconds) =>
        DateTimeOffset.FromUnixTimeSeconds(seconds);

    private static ThreadSnapshot Completed(string id, DateTimeOffset completedAt) =>
        Snapshot(id, completedAt);

    private static ThreadSnapshot Snapshot(string id, DateTimeOffset? completedAt) =>
        new(
            id,
            id,
            ThreadStatus.Idle,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            completedAt,
            null,
            completedAt,
            null,
            0,
            RolloutSourceStatus.Healthy);

    private static ThreadSnapshot Incident(
        string id,
        string episodeId,
        ServiceIncidentPhase phase,
        DateTimeOffset occurredAt) =>
        new(
            id,
            id,
            phase is ServiceIncidentPhase.Failed ? ThreadStatus.Error : ThreadStatus.Warning,
            occurredAt,
            occurredAt,
            occurredAt,
            null,
            null,
            null,
            0,
            RolloutSourceStatus.Healthy,
            serviceIncident: new ServiceIncident(
                episodeId,
                phase,
                503,
                5,
                5,
                occurredAt));
}
