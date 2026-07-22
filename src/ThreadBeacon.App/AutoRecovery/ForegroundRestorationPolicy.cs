namespace ThreadBeacon.App.AutoRecovery;

public sealed record ForegroundApplicationIdentity(
    int ProcessId,
    long StartTimeUtcTicks,
    bool IsCodex);

public enum ForegroundRestorationDecision
{
    Restore,
    SkipOriginalUnavailable,
    SkipOriginalIsCodex,
    SkipCodexUnavailable,
    SkipForegroundChanged,
}

public static class ForegroundRestorationPolicy
{
    public static ForegroundRestorationDecision Evaluate(
        ForegroundApplicationIdentity? originalApplication,
        ForegroundApplicationIdentity? codexApplication,
        ForegroundApplicationIdentity? currentForegroundApplication,
        bool isOriginalWindowAvailable)
    {
        if (originalApplication is null)
        {
            return ForegroundRestorationDecision.SkipOriginalUnavailable;
        }

        if (originalApplication.IsCodex)
        {
            return ForegroundRestorationDecision.SkipOriginalIsCodex;
        }

        if (!isOriginalWindowAvailable)
        {
            return ForegroundRestorationDecision.SkipOriginalUnavailable;
        }

        if (codexApplication is not { IsCodex: true })
        {
            return ForegroundRestorationDecision.SkipCodexUnavailable;
        }

        return currentForegroundApplication == codexApplication
            ? ForegroundRestorationDecision.Restore
            : ForegroundRestorationDecision.SkipForegroundChanged;
    }
}
