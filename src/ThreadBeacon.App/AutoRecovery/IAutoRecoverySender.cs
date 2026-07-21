namespace ThreadBeacon.App.AutoRecovery;

public interface IAutoRecoverySender
{
    Task<AutoRecoverySendResult> SendAsync(
        AutoRecoveryRequest request,
        CancellationToken cancellationToken);
}
