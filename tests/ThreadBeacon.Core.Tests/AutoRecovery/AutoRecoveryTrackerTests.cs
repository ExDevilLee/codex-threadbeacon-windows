using ThreadBeacon.Core.AutoRecovery;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;

namespace ThreadBeacon.Core.Tests.AutoRecovery;

public sealed class AutoRecoveryTrackerTests
{
    [Fact]
    public void Observe_BaselineRecordsFailedEpisodeWithoutReturningCandidate()
    {
        var tracker = new AutoRecoveryTracker();

        IReadOnlyList<AutoRecoveryCandidate> result = tracker.Observe(
            [Snapshot(ServiceIncidentPhase.Failed)],
            RefreshNotificationPolicy.Baseline);

        Assert.Empty(result);
        Assert.Equal(["thread-1:episode-1"], tracker.SeenEpisodeIds);
    }

    [Fact]
    public void Observe_RetryingEpisodeBecomesCandidateOnlyWhenItFails()
    {
        var tracker = new AutoRecoveryTracker();

        Assert.Empty(tracker.Observe(
            [Snapshot(ServiceIncidentPhase.Retrying)],
            RefreshNotificationPolicy.Notify));
        IReadOnlyList<AutoRecoveryCandidate> failed = tracker.Observe(
            [Snapshot(ServiceIncidentPhase.Failed)],
            RefreshNotificationPolicy.Notify);
        IReadOnlyList<AutoRecoveryCandidate> duplicate = tracker.Observe(
            [Snapshot(ServiceIncidentPhase.Failed)],
            RefreshNotificationPolicy.Notify);

        AutoRecoveryCandidate candidate = Assert.Single(failed);
        Assert.Equal("thread-1", candidate.ThreadId);
        Assert.Equal("episode-1", candidate.EpisodeId);
        Assert.Equal(AutoRecoveryIncidentType.Http503, candidate.IncidentType);
        Assert.Equal("Renamed title", candidate.Title);
        Assert.Equal(@"C:\Codex\rollout.jsonl", candidate.RolloutPath);
        Assert.Empty(duplicate);
    }

    [Fact]
    public void Observe_ArchivedAndMissingRolloutTasksNeverBecomeCandidates()
    {
        var tracker = new AutoRecoveryTracker();

        IReadOnlyList<AutoRecoveryCandidate> result = tracker.Observe(
            [
                Snapshot(ServiceIncidentPhase.Failed, isArchived: true),
                Snapshot(ServiceIncidentPhase.Failed, rolloutPath: null, id: "thread-2"),
            ],
            RefreshNotificationPolicy.Notify);

        Assert.Empty(result);
    }

    [Fact]
    public void Observe_StreamDisconnectFailureCreatesDedicatedCandidateOnce()
    {
        var tracker = new AutoRecoveryTracker();

        IReadOnlyList<AutoRecoveryCandidate> first = tracker.Observe(
            [Snapshot(ServiceIncidentPhase.Failed, kind: ServiceIncidentKind.StreamDisconnected)],
            RefreshNotificationPolicy.Notify);
        IReadOnlyList<AutoRecoveryCandidate> duplicate = tracker.Observe(
            [Snapshot(ServiceIncidentPhase.Failed, kind: ServiceIncidentKind.StreamDisconnected)],
            RefreshNotificationPolicy.Notify);

        Assert.Equal(AutoRecoveryIncidentType.StreamDisconnected, Assert.Single(first).IncidentType);
        Assert.Empty(duplicate);
    }

    private static ThreadSnapshot Snapshot(
        ServiceIncidentPhase phase,
        bool isArchived = false,
        string? rolloutPath = @"C:\Codex\rollout.jsonl",
        string id = "thread-1",
        ServiceIncidentKind kind = ServiceIncidentKind.ServiceUnavailable) => new(
            id,
            "Renamed title",
            phase is ServiceIncidentPhase.Failed ? ThreadStatus.Error : ThreadStatus.Warning,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            null,
            null,
            null,
            0,
            RolloutSourceStatus.Healthy,
            serviceIncident: new ServiceIncident(
                "episode-1",
                phase,
                503,
                5,
                5,
                DateTimeOffset.UnixEpoch,
                kind),
            isArchived: isArchived,
            rolloutPath: rolloutPath);
}
