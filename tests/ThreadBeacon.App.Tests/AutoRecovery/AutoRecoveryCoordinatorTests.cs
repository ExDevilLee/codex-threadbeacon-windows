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
        Assert.Equal("unexpected_error", history.Writes[^1].DiagnosticCode);
    }

    [Fact]
    public async Task ObserveAsync_RecordsStableSenderDiagnosticWithoutPromptOrPath()
    {
        var sender = new RecordingRecoverySender
        {
            Result = AutoRecoverySendResult.Failed("codex_frontmost"),
        };
        var history = new RecordingRecoveryHistoryStore();
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.English);
        settings.IsEnabled = true;
        var coordinator = new AutoRecoveryCoordinator(() => settings, sender, history);

        await coordinator.ObserveAsync([FailedSnapshot()], RefreshNotificationPolicy.Notify);

        AutoRecoveryHistoryEntry failed = history.Writes[^1];
        Assert.Equal(AutoRecoveryHistoryStatus.Failed, failed.Status);
        Assert.Equal("codex_frontmost", failed.DiagnosticCode);
        Assert.DoesNotContain("Continue", failed.DiagnosticCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Codex", failed.DiagnosticCode, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ObserveAsync_NormalizesArbitrarySenderDetail()
    {
        var sender = new RecordingRecoverySender
        {
            Result = AutoRecoverySendResult.Failed("untrusted_free_text"),
        };
        var history = new RecordingRecoveryHistoryStore();
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.English);
        settings.IsEnabled = true;
        var coordinator = new AutoRecoveryCoordinator(() => settings, sender, history);

        await coordinator.ObserveAsync([FailedSnapshot()], RefreshNotificationPolicy.Notify);

        Assert.Equal("unexpected_error", history.Writes[^1].DiagnosticCode);
    }

    [Fact]
    public async Task ObserveAsync_OpensCircuitAfterConfiguredDistinctEpisodes()
    {
        string path = Path.Combine(Path.GetTempPath(), $"circuit-{Guid.NewGuid():N}.json");
        try
        {
            var sender = new RecordingRecoverySender();
            var history = new RecordingRecoveryHistoryStore();
            var circuits = new JsonAutoRecoveryCircuitStore(path);
            AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
                AutoRecoveryPromptLanguage.English);
            settings.IsEnabled = true;
            var coordinator = new AutoRecoveryCoordinator(
                () => settings,
                sender,
                history,
                circuitStore: circuits);

            await coordinator.ObserveAsync([FailedSnapshot(episode: "episode-1")], RefreshNotificationPolicy.Notify);
            await coordinator.ObserveAsync([FailedSnapshot(episode: "episode-2")], RefreshNotificationPolicy.Notify);
            await coordinator.ObserveAsync([FailedSnapshot(episode: "episode-3")], RefreshNotificationPolicy.Notify);
            await coordinator.ObserveAsync([FailedSnapshot(episode: "episode-4")], RefreshNotificationPolicy.Notify);

            Assert.Equal(3, sender.Requests.Count);
            Assert.Equal(3, Assert.Single(circuits.Load()).AttemptCount);
            Assert.Equal(AutoRecoveryHistoryStatus.CircuitOpen, history.Writes[^1].Status);
            Assert.Equal("circuit_open", history.Writes[^1].DiagnosticCode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ObserveAsync_PreflightRejectionDoesNotCountTowardCircuit()
    {
        string path = Path.Combine(Path.GetTempPath(), $"circuit-{Guid.NewGuid():N}.json");
        try
        {
            var sender = new RecordingRecoverySender
            {
                StartsAutomation = false,
                Result = AutoRecoverySendResult.Failed("codex_frontmost"),
            };
            var circuits = new JsonAutoRecoveryCircuitStore(path);
            AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
                AutoRecoveryPromptLanguage.English);
            settings.IsEnabled = true;
            var coordinator = new AutoRecoveryCoordinator(
                () => settings,
                sender,
                circuitStore: circuits);

            await coordinator.ObserveAsync(
                [FailedSnapshot(episode: "episode-1")],
                RefreshNotificationPolicy.Notify);

            Assert.Empty(circuits.Load());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ObserveAsync_NewerCompletionResetsCircuit()
    {
        string path = Path.Combine(Path.GetTempPath(), $"circuit-{Guid.NewGuid():N}.json");
        try
        {
            var circuits = new JsonAutoRecoveryCircuitStore(path);
            circuits.RecordAttempt(
                new AutoRecoveryCandidate(
                    "thread-1",
                    "episode-1",
                    AutoRecoveryIncidentType.Http400,
                    "Title",
                    @"C:\Codex\thread-1.jsonl",
                    DateTimeOffset.FromUnixTimeSeconds(10)),
                DateTimeOffset.FromUnixTimeSeconds(10));
            var coordinator = new AutoRecoveryCoordinator(
                () => AutoRecoverySettings.CreateDefault(AutoRecoveryPromptLanguage.English),
                new RecordingRecoverySender(),
                circuitStore: circuits);
            ThreadSnapshot failed = FailedSnapshot();
            var completed = new ThreadSnapshot(
                failed.Id,
                failed.Title,
                ThreadStatus.JustCompleted,
                DateTimeOffset.FromUnixTimeSeconds(11),
                DateTimeOffset.FromUnixTimeSeconds(11),
                DateTimeOffset.FromUnixTimeSeconds(11),
                DateTimeOffset.FromUnixTimeSeconds(10),
                DateTimeOffset.FromUnixTimeSeconds(11),
                null,
                0,
                RolloutSourceStatus.Healthy,
                rolloutPath: failed.RolloutPath);

            await coordinator.ObserveAsync([completed], RefreshNotificationPolicy.Baseline);

            Assert.Empty(circuits.Load());
        }
        finally
        {
            File.Delete(path);
        }
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

    public AutoRecoverySendResult Result { get; init; } = AutoRecoverySendResult.Sent;

    public bool StartsAutomation { get; init; } = true;

    public int MaximumConcurrency { get; private set; }

    public async Task<AutoRecoverySendResult> SendAsync(
        AutoRecoveryRequest request,
        Action automationStarted,
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

            if (StartsAutomation)
            {
                automationStarted();
            }

            return Result;
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
