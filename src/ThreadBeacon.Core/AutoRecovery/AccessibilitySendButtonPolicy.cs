namespace ThreadBeacon.Core.AutoRecovery;

public sealed record AccessibilityButtonCandidate(
    string Name,
    string AutomationId,
    bool IsEnabled,
    bool SupportsInvoke,
    bool IsNearComposer,
    string ClassName);

public static class AccessibilitySendButtonPolicy
{
    public static int? Select(IReadOnlyList<AccessibilityButtonCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        int[] matches = candidates
            .Select((candidate, index) => (candidate, index))
            .Where(pair => IsSendButton(pair.candidate))
            .Select(pair => pair.index)
            .ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private static bool IsSendButton(AccessibilityButtonCandidate candidate) =>
        candidate.Name.Length == 0
        && candidate.AutomationId.Length == 0
        && candidate.IsEnabled
        && candidate.SupportsInvoke
        && candidate.IsNearComposer
        && candidate.ClassName.Contains("size-token-button-composer", StringComparison.Ordinal)
        && candidate.ClassName.Contains("bg-token-foreground", StringComparison.Ordinal);
}
