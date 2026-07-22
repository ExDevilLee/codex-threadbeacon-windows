using ThreadBeacon.App.AutoRecovery;

namespace ThreadBeacon.App.Tests.AutoRecovery;

public sealed class ForegroundRestorationPolicyTests
{
    private static readonly ForegroundApplicationIdentity Original = new(10, 100, false);
    private static readonly ForegroundApplicationIdentity Codex = new(20, 200, true);

    [Fact]
    public void Evaluate_RestoresOnlyWhenCurrentProcessIsCapturedCodex()
    {
        ForegroundRestorationDecision decision = ForegroundRestorationPolicy.Evaluate(
            Original,
            Codex,
            Codex,
            isOriginalWindowAvailable: true);

        Assert.Equal(ForegroundRestorationDecision.Restore, decision);
    }

    [Theory]
    [InlineData(false, false, ForegroundRestorationDecision.SkipOriginalUnavailable)]
    [InlineData(true, false, ForegroundRestorationDecision.SkipOriginalIsCodex)]
    public void Evaluate_SkipsInvalidOriginal(
        bool originalIsCodex,
        bool isOriginalWindowAvailable,
        ForegroundRestorationDecision expected)
    {
        ForegroundApplicationIdentity original = Original with { IsCodex = originalIsCodex };

        ForegroundRestorationDecision decision = ForegroundRestorationPolicy.Evaluate(
            original,
            Codex,
            Codex,
            isOriginalWindowAvailable);

        Assert.Equal(expected, decision);
    }

    [Fact]
    public void Evaluate_SkipsWhenUserSelectedAnotherApplication()
    {
        var thirdApplication = new ForegroundApplicationIdentity(30, 300, false);

        ForegroundRestorationDecision decision = ForegroundRestorationPolicy.Evaluate(
            Original,
            Codex,
            thirdApplication,
            isOriginalWindowAvailable: true);

        Assert.Equal(ForegroundRestorationDecision.SkipForegroundChanged, decision);
    }

    [Fact]
    public void Evaluate_SkipsReusedCodexProcessId()
    {
        ForegroundApplicationIdentity reusedPid = Codex with { StartTimeUtcTicks = 201 };

        ForegroundRestorationDecision decision = ForegroundRestorationPolicy.Evaluate(
            Original,
            Codex,
            reusedPid,
            isOriginalWindowAvailable: true);

        Assert.Equal(ForegroundRestorationDecision.SkipForegroundChanged, decision);
    }

    [Fact]
    public void Evaluate_SkipsMissingIdentities()
    {
        Assert.Equal(
            ForegroundRestorationDecision.SkipOriginalUnavailable,
            ForegroundRestorationPolicy.Evaluate(null, Codex, Codex, true));
        Assert.Equal(
            ForegroundRestorationDecision.SkipCodexUnavailable,
            ForegroundRestorationPolicy.Evaluate(Original, null, Codex, true));
        Assert.Equal(
            ForegroundRestorationDecision.SkipForegroundChanged,
            ForegroundRestorationPolicy.Evaluate(Original, Codex, null, true));
    }

    [Fact]
    public void Evaluate_SkipsIdentityThatIsNotCodex()
    {
        ForegroundApplicationIdentity other = Codex with { IsCodex = false };

        ForegroundRestorationDecision decision = ForegroundRestorationPolicy.Evaluate(
            Original,
            other,
            other,
            isOriginalWindowAvailable: true);

        Assert.Equal(ForegroundRestorationDecision.SkipCodexUnavailable, decision);
    }
}
