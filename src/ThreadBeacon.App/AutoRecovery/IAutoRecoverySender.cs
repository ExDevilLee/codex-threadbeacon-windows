namespace ThreadBeacon.App.AutoRecovery;

public interface IAutoRecoverySender
{
    Task<AutoRecoverySendResult> SendAsync(
        AutoRecoveryRequest request,
        Action automationStarted,
        CancellationToken cancellationToken);
}
