using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.AutoRecovery;

public interface IRolloutRecoveryEvidenceMonitor
{
    RolloutRecoveryCheckpoint Capture(string rolloutPath);

    Task<bool> WaitForEvidenceAsync(
        string rolloutPath,
        RolloutRecoveryCheckpoint checkpoint,
        string expectedMessage,
        CancellationToken cancellationToken);
}

public sealed class RolloutRecoveryEvidenceMonitor : IRolloutRecoveryEvidenceMonitor
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

    public RolloutRecoveryCheckpoint Capture(string rolloutPath) =>
        RolloutRecoveryVerifier.Capture(rolloutPath);

    public async Task<bool> WaitForEvidenceAsync(
        string rolloutPath,
        RolloutRecoveryCheckpoint checkpoint,
        string expectedMessage,
        CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + Timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (RolloutRecoveryVerifier.ReadEvidence(
                    rolloutPath,
                    checkpoint,
                    expectedMessage).IsVerified)
            {
                return true;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}
