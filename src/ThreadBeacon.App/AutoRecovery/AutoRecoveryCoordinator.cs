using ThreadBeacon.Core.AutoRecovery;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;

namespace ThreadBeacon.App.AutoRecovery;

public sealed class AutoRecoveryCoordinator : IAutoRecoveryObserver
{
    private readonly Func<AutoRecoverySettings> settingsProvider;
    private readonly IAutoRecoverySender sender;
    private readonly IAutoRecoveryHistoryStore? historyStore;
    private readonly TimeProvider timeProvider;
    private readonly AutoRecoveryTracker tracker = new();
    private readonly SemaphoreSlim observationGate = new(1, 1);

    public AutoRecoveryCoordinator(
        Func<AutoRecoverySettings> settingsProvider,
        IAutoRecoverySender sender,
        IAutoRecoveryHistoryStore? historyStore = null,
        TimeProvider? timeProvider = null)
    {
        this.settingsProvider = settingsProvider
            ?? throw new ArgumentNullException(nameof(settingsProvider));
        this.sender = sender ?? throw new ArgumentNullException(nameof(sender));
        this.historyStore = historyStore;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task ObserveAsync(
        IReadOnlyList<ThreadSnapshot> snapshots,
        RefreshNotificationPolicy policy,
        CancellationToken cancellationToken = default)
    {
        await observationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IReadOnlyList<AutoRecoveryCandidate> candidates = tracker.Observe(snapshots, policy);
            foreach (AutoRecoveryCandidate candidate in candidates)
            {
                AutoRecoveryDecision decision = AutoRecoveryPolicy.Evaluate(
                    candidate,
                    settingsProvider());
                if (decision is not { Kind: AutoRecoveryDecisionKind.Send, Prompt: not null })
                {
                    continue;
                }

                DateTimeOffset startedAt = timeProvider.GetUtcNow();
                var history = new AutoRecoveryHistoryEntry(
                    Guid.NewGuid().ToString("N"),
                    candidate.ThreadId,
                    candidate.EpisodeId,
                    candidate.IncidentType,
                    AutoRecoveryHistoryStatus.Sending,
                    startedAt,
                    startedAt);
                TryWriteHistory(history);
                try
                {
                    AutoRecoverySendResult result = await sender.SendAsync(
                        new AutoRecoveryRequest(candidate, decision.Prompt),
                        cancellationToken).ConfigureAwait(false);
                    TryWriteHistory(history with
                    {
                        Status = result.Status is AutoRecoverySendStatus.Sent
                            ? AutoRecoveryHistoryStatus.Sent
                            : AutoRecoveryHistoryStatus.Failed,
                        UpdatedAt = timeProvider.GetUtcNow(),
                    });
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    TryWriteHistory(history with
                    {
                        Status = AutoRecoveryHistoryStatus.Failed,
                        UpdatedAt = timeProvider.GetUtcNow(),
                    });
                    // Recovery is best effort and must never break the refresh loop.
                }
            }
        }
        finally
        {
            observationGate.Release();
        }
    }

    private void TryWriteHistory(AutoRecoveryHistoryEntry entry)
    {
        try
        {
            historyStore?.Upsert(entry);
        }
        catch
        {
            // History persistence must not affect monitoring or recovery.
        }
    }
}
