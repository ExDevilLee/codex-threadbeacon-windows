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
    private readonly IAutoRecoveryCircuitStore? circuitStore;
    private readonly AutoRecoveryTracker tracker = new();
    private readonly SemaphoreSlim observationGate = new(1, 1);

    public AutoRecoveryCoordinator(
        Func<AutoRecoverySettings> settingsProvider,
        IAutoRecoverySender sender,
        IAutoRecoveryHistoryStore? historyStore = null,
        TimeProvider? timeProvider = null,
        IAutoRecoveryCircuitStore? circuitStore = null)
    {
        this.settingsProvider = settingsProvider
            ?? throw new ArgumentNullException(nameof(settingsProvider));
        this.sender = sender ?? throw new ArgumentNullException(nameof(sender));
        this.historyStore = historyStore;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.circuitStore = circuitStore;
    }

    public async Task ObserveAsync(
        IReadOnlyList<ThreadSnapshot> snapshots,
        RefreshNotificationPolicy policy,
        CancellationToken cancellationToken = default)
    {
        await observationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObserveCompletions(snapshots);
            IReadOnlyList<AutoRecoveryCandidate> candidates = tracker.Observe(snapshots, policy);
            foreach (AutoRecoveryCandidate candidate in candidates)
            {
                AutoRecoveryCircuitState? circuit = TryGetCircuit(candidate);
                AutoRecoveryDecision decision = AutoRecoveryPolicy.Evaluate(
                    candidate,
                    settingsProvider(),
                    circuit?.AttemptCount ?? 0);
                if (decision.Kind is AutoRecoveryDecisionKind.CircuitOpen)
                {
                    DateTimeOffset occurredAt = timeProvider.GetUtcNow();
                    TryWriteHistory(new AutoRecoveryHistoryEntry(
                        Guid.NewGuid().ToString("N"),
                        candidate.ThreadId,
                        candidate.EpisodeId,
                        candidate.IncidentType,
                        AutoRecoveryHistoryStatus.CircuitOpen,
                        occurredAt,
                        occurredAt,
                        "circuit_open"));
                    continue;
                }

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
                        () => TryRecordAttempt(candidate, timeProvider.GetUtcNow()),
                        cancellationToken).ConfigureAwait(false);
                    TryWriteHistory(history with
                    {
                        Status = result.Status is AutoRecoverySendStatus.Sent
                            ? AutoRecoveryHistoryStatus.Sent
                            : AutoRecoveryHistoryStatus.Failed,
                        UpdatedAt = timeProvider.GetUtcNow(),
                        DiagnosticCode = result.Status is AutoRecoverySendStatus.Sent
                            ? null
                            : AutoRecoveryDiagnosticCodes.Normalize(result.DiagnosticCode),
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
                        DiagnosticCode = AutoRecoveryDiagnosticCodes.UnexpectedError,
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

    private void ObserveCompletions(IReadOnlyList<ThreadSnapshot> snapshots)
    {
        if (circuitStore is null)
        {
            return;
        }

        foreach (ThreadSnapshot snapshot in snapshots)
        {
            if (snapshot.CompletionEventAt is DateTimeOffset completedAt)
            {
                try
                {
                    circuitStore.ObserveCompletion(snapshot.Id, completedAt);
                }
                catch
                {
                    // Circuit state is best effort and never breaks monitoring.
                }
            }
        }
    }

    private AutoRecoveryCircuitState? TryGetCircuit(AutoRecoveryCandidate candidate)
    {
        try
        {
            return circuitStore?.StateFor(candidate.ThreadId, candidate.IncidentType);
        }
        catch
        {
            return null;
        }
    }

    private void TryRecordAttempt(AutoRecoveryCandidate candidate, DateTimeOffset attemptedAt)
    {
        try
        {
            circuitStore?.RecordAttempt(candidate, attemptedAt);
        }
        catch
        {
            // Circuit state is best effort and never blocks a configured recovery.
        }
    }
}
