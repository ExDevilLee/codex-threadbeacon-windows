namespace ThreadBeacon.Core.AutoRecovery;

public enum AutoRecoveryDecisionKind
{
    Disabled,
    Send,
}

public sealed record AutoRecoveryDecision(
    AutoRecoveryDecisionKind Kind,
    string? Prompt = null)
{
    public static AutoRecoveryDecision Disabled { get; } = new(
        AutoRecoveryDecisionKind.Disabled);
}

public static class AutoRecoveryPolicy
{
    public static AutoRecoveryDecision Evaluate(
        AutoRecoveryCandidate candidate,
        AutoRecoverySettings settings)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(settings);

        AutoRecoveryRule rule = settings.RuleFor(candidate.IncidentType);
        return settings.IsEnabled && rule.IsEnabled
            ? new AutoRecoveryDecision(AutoRecoveryDecisionKind.Send, rule.Prompt)
            : AutoRecoveryDecision.Disabled;
    }
}
