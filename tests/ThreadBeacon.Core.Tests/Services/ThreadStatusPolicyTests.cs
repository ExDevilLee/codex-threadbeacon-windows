using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class ThreadStatusPolicyTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(10_000);

    [Fact]
    public void Evaluate_KeepsRecentCompletion()
    {
        DateTimeOffset completedAt = Now.AddSeconds(-30);
        var observation = Observation(
            ThreadStatus.JustCompleted,
            completedAt,
            completedAt);

        ThreadDisplayState result = ThreadStatusPolicy.Evaluate(observation, Now, Now);

        Assert.Equal(ThreadStatus.JustCompleted, result.Status);
        Assert.Equal(completedAt, result.ChangedAt);
    }

    [Fact]
    public void Evaluate_ChangesOldCompletionToIdle()
    {
        DateTimeOffset completedAt = Now.AddSeconds(-61);
        var observation = Observation(
            ThreadStatus.JustCompleted,
            completedAt,
            completedAt);

        ThreadDisplayState result = ThreadStatusPolicy.Evaluate(observation, Now, Now);

        Assert.Equal(ThreadStatus.Idle, result.Status);
        Assert.Equal(completedAt, result.ChangedAt);
    }

    [Fact]
    public void Evaluate_ChangesStaleRunningToUnknown()
    {
        DateTimeOffset latestEventAt = Now.AddSeconds(-121);
        var observation = Observation(
            ThreadStatus.Running,
            Now.AddSeconds(-200),
            latestEventAt);

        ThreadDisplayState result = ThreadStatusPolicy.Evaluate(observation, Now, Now);

        Assert.Equal(ThreadStatus.Unknown, result.Status);
        Assert.Equal(latestEventAt, result.ChangedAt);
    }

    [Fact]
    public void Evaluate_UsesFallbackWhenObservationHasNoChangeTime()
    {
        DateTimeOffset fallback = Now.AddHours(-1);

        ThreadDisplayState result = ThreadStatusPolicy.Evaluate(
            RolloutObservation.Empty,
            fallback,
            Now);

        Assert.Equal(ThreadStatus.Unknown, result.Status);
        Assert.Equal(fallback, result.ChangedAt);
    }

    private static RolloutObservation Observation(
        ThreadStatus status,
        DateTimeOffset changedAt,
        DateTimeOffset latestEventAt) =>
        new(status, changedAt, latestEventAt, null, null, null);
}
