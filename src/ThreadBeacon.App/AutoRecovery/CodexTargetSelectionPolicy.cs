namespace ThreadBeacon.App.AutoRecovery;

public enum CodexComposerValueState
{
    Empty,
    NotEmpty,
    Unavailable,
}

public enum CodexTargetPreflightAction
{
    Reject,
    Navigate,
    UseCurrent,
}

public sealed record CodexTargetPreflightResult(
    CodexTargetPreflightAction Action,
    CodexTargetSelectionFailure? Failure = null,
    int? ObservedCount = null)
{
    public string? DiagnosticCode => Failure is CodexTargetSelectionFailure failure
        ? CodexTargetSelectionResult.Failed(failure, ObservedCount).DiagnosticCode
        : null;
}

public static class CodexTargetSelectionPolicy
{
    public static CodexTargetPreflightResult Evaluate(
        CodexTargetSelectionMode mode,
        bool isCodexForeground,
        int currentTitleMatchCount,
        int sourceComposerCount,
        CodexComposerValueState sourceComposerValue)
    {
        if (mode is CodexTargetSelectionMode.Unattended
            && isCodexForeground
            && currentTitleMatchCount != 1)
        {
            return Reject(CodexTargetSelectionFailure.CodexForeground);
        }

        if (sourceComposerCount != 1)
        {
            return Reject(
                CodexTargetSelectionFailure.SourceComposerNotUnique,
                sourceComposerCount);
        }

        if (sourceComposerValue is CodexComposerValueState.Unavailable)
        {
            return Reject(CodexTargetSelectionFailure.SourceComposerValueUnavailable);
        }

        if (sourceComposerValue is CodexComposerValueState.NotEmpty)
        {
            return Reject(CodexTargetSelectionFailure.SourceComposerNotEmpty);
        }

        return mode is CodexTargetSelectionMode.Unattended && isCodexForeground
            ? new CodexTargetPreflightResult(CodexTargetPreflightAction.UseCurrent)
            : new CodexTargetPreflightResult(CodexTargetPreflightAction.Navigate);
    }

    private static CodexTargetPreflightResult Reject(
        CodexTargetSelectionFailure failure,
        int? observedCount = null) =>
        new(CodexTargetPreflightAction.Reject, failure, observedCount);
}
