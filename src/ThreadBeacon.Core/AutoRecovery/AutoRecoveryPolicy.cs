namespace ThreadBeacon.Core.AutoRecovery;

public enum AutoRecoveryDecisionKind
{
    Disabled,
    CircuitOpen,
    Send,
}

public sealed record AutoRecoveryDecision(
    AutoRecoveryDecisionKind Kind,
    string? Prompt = null,
    int? AttemptCount = null,
    int? AttemptLimit = null)
{
    public static AutoRecoveryDecision Disabled { get; } = new(
        AutoRecoveryDecisionKind.Disabled);
}

public static class AutoRecoveryPolicy
{
    public static AutoRecoveryDecision Evaluate(
        AutoRecoveryCandidate candidate,
        AutoRecoverySettings settings,
        int consecutiveAttempts = 0)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(settings);

        AutoRecoveryRule rule = settings.RuleFor(candidate.IncidentType);
        if (!settings.IsEnabled || !rule.IsEnabled)
        {
            return AutoRecoveryDecision.Disabled;
        }

        if (rule.IsCircuitBreakerEnabled
            && consecutiveAttempts >= rule.MaximumConsecutiveAttempts)
        {
            return new AutoRecoveryDecision(
                AutoRecoveryDecisionKind.CircuitOpen,
                rule.Prompt,
                consecutiveAttempts,
                rule.MaximumConsecutiveAttempts);
        }

        return new AutoRecoveryDecision(AutoRecoveryDecisionKind.Send, rule.Prompt);
    }
}
