using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.Core.Tests.AutoRecovery;

public sealed class AccessibilitySendButtonPolicyTests
{
    [Fact]
    public void Select_ReturnsUniqueStructurallyVerifiedSendButton()
    {
        AccessibilityButtonCandidate[] candidates =
        [
            Candidate("Full access", "composer-control"),
            Candidate(string.Empty, "size-token-button-composer bg-token-foreground"),
        ];

        int? selected = AccessibilitySendButtonPolicy.Select(candidates);

        Assert.Equal(1, selected);
    }

    [Fact]
    public void Select_RejectsNamedStopButton()
    {
        AccessibilityButtonCandidate[] candidates =
        [
            Candidate("Stop", "size-token-button-composer bg-token-foreground"),
        ];

        Assert.Null(AccessibilitySendButtonPolicy.Select(candidates));
    }

    [Fact]
    public void Select_RejectsAmbiguousStructuralMatches()
    {
        AccessibilityButtonCandidate[] candidates =
        [
            Candidate(string.Empty, "size-token-button-composer bg-token-foreground"),
            Candidate(string.Empty, "size-token-button-composer bg-token-foreground"),
        ];

        Assert.Null(AccessibilitySendButtonPolicy.Select(candidates));
    }

    private static AccessibilityButtonCandidate Candidate(string name, string className) => new(
        name,
        string.Empty,
        IsEnabled: true,
        SupportsInvoke: true,
        IsNearComposer: true,
        className);
}
