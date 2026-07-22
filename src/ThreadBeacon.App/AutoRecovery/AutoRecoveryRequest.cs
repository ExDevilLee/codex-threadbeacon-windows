using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.AutoRecovery;

public sealed record AutoRecoveryRequest(
    AutoRecoveryCandidate Candidate,
    string Prompt);

public enum AutoRecoverySendStatus
{
    Sent,
    Failed,
}

public sealed record AutoRecoverySendResult(
    AutoRecoverySendStatus Status,
    string? DiagnosticCode = null)
{
    public static AutoRecoverySendResult Sent { get; } = new(
        AutoRecoverySendStatus.Sent);

    public static AutoRecoverySendResult Failed(string? diagnosticCode = null) => new(
        AutoRecoverySendStatus.Failed,
        diagnosticCode);
}
