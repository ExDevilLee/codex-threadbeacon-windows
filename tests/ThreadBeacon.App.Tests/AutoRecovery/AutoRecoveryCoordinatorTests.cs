using ThreadBeacon.App.AutoRecovery;
using ThreadBeacon.Core.AutoRecovery;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;

namespace ThreadBeacon.App.Tests.AutoRecovery;

public sealed class AutoRecoveryCoordinatorTests
{
    [Fact]
    public async Task ObserveAsync_BaselineDoesNotSendHistoricalFailure()
    {
        var sender = new RecordingRecoverySender();
        var coordinator = Coordinator(sender, enabled: true);

        await coordinator.ObserveAsync(
            [FailedSnapshot()],
            RefreshNotificationPolicy.Baseline);

        Assert.Empty(sender.Requests);
    }

    [Fact]
    public async Task ObserveAsync_InterruptedTaskWithoutServiceIncidentDoesNotSend()
    {
        var sender = new RecordingRecoverySender();
        var coordinator = Coordinator(sender, enabled: true);
        ThreadSnapshot interrupted = new(
            "thread-1",
            "Renamed title",
            ThreadStatus.Interrupted,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            null,
            null,
            null,
            0,
            RolloutSourceStatus.Healthy,
            rolloutPath: @"C:\Codex\thread-1.jsonl");

        await coordinator.ObserveAsync([interrupted], RefreshNotificationPolicy.Notify);

        Assert.Empty(sender.Requests);
    }

    [Fact]
    public async Task ObserveAsync_SendsNewFailureOnlyOnceWithConfiguredPrompt()
    {
        var sender = new RecordingRecoverySender();
        var settings = AutoRecoverySettings.CreateDefault(AutoRecoveryPromptLanguage.English);
        settings.IsEnabled = true;
        settings.SetRule(
            AutoRecoveryIncidentType.Http400,
            new AutoRecoveryRule(true, "Continue the test.", AutoRecoveryPromptSource.Custom));
        var coordinator = new AutoRecoveryCoordinator(() => settings, sender);
        ThreadSnapshot snapshot = FailedSnapshot();

        await coordinator.ObserveAsync([snapshot], RefreshNotificationPolicy.Notify);
        await coordinator.ObserveAsync([snapshot], RefreshNotificationPolicy.Notify);

        AutoRecoveryRequest request = Assert.Single(sender.Requests);
        Assert.Equal("thread-1", request.Candidate.ThreadId);
        Assert.Equal("Continue the test.", request.Prompt);
    }

    [Fact]
    public async Task ObserveAsync_DisabledSettingsStillDeduplicateEpisode()
    {
        var sender = new RecordingRecoverySender();
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.English);
        var coordinator = new AutoRecoveryCoordinator(() => settings, sender);
        ThreadSnapshot snapshot = FailedSnapshot();

        await coordinator.ObserveAsync([snapshot], RefreshNotificationPolicy.Notify);
        settings.IsEnabled = true;
        await coordinator.ObserveAsync([snapshot], RefreshNotificationPolicy.Notify);

        Assert.Empty(sender.Requests);
    }

    [Fact]
    public async Task ObserveAsync_SenderFailureDoesNotEscapeRefreshPath()
    {
        var sender = new RecordingRecoverySender { ThrowOnSend = true };
        var coordinator = Coordinator(sender, enabled: true);

        Exception? exception = await Record.ExceptionAsync(() => coordinator.ObserveAsync(
            [FailedSnapshot()],
            RefreshNotificationPolicy.Notify));

        Assert.Null(exception);
        Assert.Single(sender.Requests);
    }

    [Fact]
    public async Task ObserveAsync_ConcurrentCallsNeverOverlapSender()
    {
        var sender = new RecordingRecoverySender { Delay = TimeSpan.FromMilliseconds(50) };
        var coordinator = Coordinator(sender, enabled: true);

        await Task.WhenAll(
            coordinator.ObserveAsync([FailedSnapshot("thread-1", "episode-1")], RefreshNotificationPolicy.Notify),
            coordinator.ObserveAsync([FailedSnapshot("thread-2", "episode-2")], RefreshNotificationPolicy.Notify));

        Assert.Equal(1, sender.MaximumConcurrency);
        Assert.Equal(2, sender.Requests.Count);
    }

    [Fact]
    public async Task ObserveAsync_RecordsSendingAndFinalStatusWithoutPromptOrPath()
    {
        var sender = new RecordingRecoverySender();
        var history = new RecordingRecoveryHistoryStore();
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.English);
        settings.IsEnabled = true;
        var coordinator = new AutoRecoveryCoordinator(
            () => settings,
            sender,
            history,
            new FixedAutoRecoveryTimeProvider(DateTimeOffset.UnixEpoch));

        await coordinator.ObserveAsync([FailedSnapshot()], RefreshNotificationPolicy.Notify);

        Assert.Equal(2, history.Writes.Count);
        Assert.Equal(AutoRecoveryHistoryStatus.Sending, history.Writes[0].Status);
        Assert.Equal(AutoRecoveryHistoryStatus.Sent, history.Writes[1].Status);
        Assert.Equal("thread-1", history.Writes[1].ThreadId);
        Assert.Equal("episode-1", history.Writes[1].EpisodeId);
    }

    [Fact]
    public async Task ObserveAsync_RecordsFailedWhenSenderThrows()
    {
        var sender = new RecordingRecoverySender { ThrowOnSend = true };
        var history = new RecordingRecoveryHistoryStore();
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.English);
        settings.IsEnabled = true;
        var coordinator = new AutoRecoveryCoordinator(() => settings, sender, history);

        await coordinator.ObserveAsync([FailedSnapshot()], RefreshNotificationPolicy.Notify);

        Assert.Equal(AutoRecoveryHistoryStatus.Failed, history.Writes[^1].Status);
    }

    private static AutoRecoveryCoordinator Coordinator(
        RecordingRecoverySender sender,
        bool enabled)
    {
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.English);
        settings.IsEnabled = enabled;
        return new AutoRecoveryCoordinator(() => settings, sender);
    }

    private static ThreadSnapshot FailedSnapshot(
        string id = "thread-1",
        string episode = "episode-1") => new(
            id,
            "Renamed title",
            ThreadStatus.Error,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            null,
            null,
            null,
            0,
            RolloutSourceStatus.Healthy,
            serviceIncident: new ServiceIncident(
                episode,
                ServiceIncidentPhase.Failed,
                400,
                null,
                null,
                DateTimeOffset.UnixEpoch,
                ServiceIncidentKind.BadRequest),
            rolloutPath: $@"C:\Codex\{id}.jsonl");
}

internal sealed class RecordingRecoverySender : IAutoRecoverySender
{
    private int concurrency;

    public List<AutoRecoveryRequest> Requests { get; } = [];

    public bool ThrowOnSend { get; init; }

    public TimeSpan Delay { get; init; }

    public int MaximumConcurrency { get; private set; }

    public async Task<AutoRecoverySendResult> SendAsync(
        AutoRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        int current = Interlocked.Increment(ref concurrency);
        MaximumConcurrency = Math.Max(MaximumConcurrency, current);
        try
        {
            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            if (ThrowOnSend)
            {
                throw new InvalidOperationException("Synthetic sender failure.");
            }

            return AutoRecoverySendResult.Sent;
        }
        finally
        {
            Interlocked.Decrement(ref concurrency);
        }
    }
}

internal sealed class RecordingRecoveryHistoryStore : IAutoRecoveryHistoryStore
{
    public List<AutoRecoveryHistoryEntry> Writes { get; } = [];

    public IReadOnlyList<AutoRecoveryHistoryEntry> Load() => Writes;

    public bool Upsert(AutoRecoveryHistoryEntry entry)
    {
        Writes.Add(entry);
        return true;
    }
}

internal sealed class FixedAutoRecoveryTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
