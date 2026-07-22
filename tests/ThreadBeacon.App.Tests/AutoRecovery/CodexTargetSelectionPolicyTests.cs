using ThreadBeacon.App.AutoRecovery;

namespace ThreadBeacon.App.Tests.AutoRecovery;

public sealed class CodexTargetSelectionPolicyTests
{
    [Fact]
    public void Evaluate_AllowsConfirmedForegroundTargetWithEmptyComposer()
    {
        CodexTargetPreflightResult result = CodexTargetSelectionPolicy.Evaluate(
            CodexTargetSelectionMode.Unattended,
            isCodexForeground: true,
            currentTitleMatchCount: 1,
            sourceComposerCount: 1,
            CodexComposerValueState.Empty);

        Assert.Equal(CodexTargetPreflightAction.UseCurrent, result.Action);
        Assert.Null(result.Failure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Evaluate_RejectsUnconfirmedForegroundTarget(int titleMatchCount)
    {
        CodexTargetPreflightResult result = CodexTargetSelectionPolicy.Evaluate(
            CodexTargetSelectionMode.Unattended,
            isCodexForeground: true,
            currentTitleMatchCount: titleMatchCount,
            sourceComposerCount: 1,
            CodexComposerValueState.Empty);

        Assert.Equal(CodexTargetPreflightAction.Reject, result.Action);
        Assert.Equal(CodexTargetSelectionFailure.CodexForeground, result.Failure);
    }

    [Fact]
    public void Evaluate_PreservesDraftOnConfirmedForegroundTarget()
    {
        CodexTargetPreflightResult result = CodexTargetSelectionPolicy.Evaluate(
            CodexTargetSelectionMode.Unattended,
            isCodexForeground: true,
            currentTitleMatchCount: 1,
            sourceComposerCount: 1,
            CodexComposerValueState.NotEmpty);

        Assert.Equal(CodexTargetPreflightAction.Reject, result.Action);
        Assert.Equal(CodexTargetSelectionFailure.SourceComposerNotEmpty, result.Failure);
    }

    [Fact]
    public void Evaluate_RejectsUnreadableComposer()
    {
        CodexTargetPreflightResult result = CodexTargetSelectionPolicy.Evaluate(
            CodexTargetSelectionMode.Unattended,
            isCodexForeground: true,
            currentTitleMatchCount: 1,
            sourceComposerCount: 1,
            CodexComposerValueState.Unavailable);

        Assert.Equal(CodexTargetSelectionFailure.SourceComposerValueUnavailable, result.Failure);
    }

    [Theory]
    [InlineData(0, "source_composer_count_0")]
    [InlineData(2, "source_composer_count_many")]
    public void Evaluate_RejectsNonUniqueComposerWithBoundedDiagnostic(
        int composerCount,
        string expectedCode)
    {
        CodexTargetPreflightResult result = CodexTargetSelectionPolicy.Evaluate(
            CodexTargetSelectionMode.Unattended,
            isCodexForeground: false,
            currentTitleMatchCount: 0,
            sourceComposerCount: composerCount,
            CodexComposerValueState.Unavailable);

        Assert.Equal(CodexTargetSelectionFailure.SourceComposerNotUnique, result.Failure);
        Assert.Equal(expectedCode, result.DiagnosticCode);
    }

    [Theory]
    [InlineData(CodexTargetSelectionMode.Interactive, true)]
    [InlineData(CodexTargetSelectionMode.Unattended, false)]
    public void Evaluate_NavigatesWhenForegroundShortcutDoesNotApply(
        CodexTargetSelectionMode mode,
        bool isForeground)
    {
        CodexTargetPreflightResult result = CodexTargetSelectionPolicy.Evaluate(
            mode,
            isForeground,
            currentTitleMatchCount: 1,
            sourceComposerCount: 1,
            CodexComposerValueState.Empty);

        Assert.Equal(CodexTargetPreflightAction.Navigate, result.Action);
    }

    [Theory]
    [InlineData(CodexTargetSelectionFailure.TargetHeaderNotUnique, 0, "target_header_count_0")]
    [InlineData(CodexTargetSelectionFailure.TargetHeaderNotUnique, 9, "target_header_count_many")]
    [InlineData(CodexTargetSelectionFailure.TargetComposerNotUnique, 2, "composer_count_many")]
    [InlineData(CodexTargetSelectionFailure.CodexForeground, null, "codex_frontmost")]
    public void SelectionResult_ExposesStableDiagnosticCodes(
        CodexTargetSelectionFailure failure,
        int? observedCount,
        string expectedCode)
    {
        CodexTargetSelectionResult result = CodexTargetSelectionResult.Failed(
            failure,
            observedCount);

        Assert.Equal(expectedCode, result.DiagnosticCode);
        Assert.False(result.IsSelected);
    }
}
