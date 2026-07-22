namespace ThreadBeacon.App.AutoRecovery;

public sealed record CodexComposerSession(string Id);

public enum CodexTargetSelectionMode
{
    Interactive,
    Unattended,
}

public enum CodexTargetSelectionFailure
{
    InvalidThreadId,
    CodexNotRunning,
    CodexWindowNotUnique,
    CodexForeground,
    SourceComposerNotUnique,
    SourceComposerValueUnavailable,
    SourceComposerNotEmpty,
    DeepLinkFailed,
    TargetHeaderNotUnique,
    TargetComposerNotUnique,
    TargetComposerValueUnavailable,
    TargetComposerNotEmpty,
    TargetDidNotChange,
    SelectionTimedOut,
}

public sealed record CodexTargetSelectionResult
{
    private CodexTargetSelectionResult(
        CodexComposerSession? session,
        CodexTargetSelectionFailure? failure,
        int? observedCount)
    {
        Session = session;
        Failure = failure;
        ObservedCount = observedCount;
    }

    public CodexComposerSession? Session { get; }

    public CodexTargetSelectionFailure? Failure { get; }

    public int? ObservedCount { get; }

    public bool IsSelected => Session is not null && Failure is null;

    public string DiagnosticCode => Failure switch
    {
        null when IsSelected => "selected",
        CodexTargetSelectionFailure.InvalidThreadId => "invalid_thread_id",
        CodexTargetSelectionFailure.CodexNotRunning => "codex_not_running",
        CodexTargetSelectionFailure.CodexWindowNotUnique => $"codex_window_count_{BoundedCount(ObservedCount)}",
        CodexTargetSelectionFailure.CodexForeground => "codex_frontmost",
        CodexTargetSelectionFailure.SourceComposerNotUnique => $"source_composer_count_{BoundedCount(ObservedCount)}",
        CodexTargetSelectionFailure.SourceComposerValueUnavailable => "source_composer_value_unavailable",
        CodexTargetSelectionFailure.SourceComposerNotEmpty => "source_composer_not_empty",
        CodexTargetSelectionFailure.DeepLinkFailed => "deep_link_failed",
        CodexTargetSelectionFailure.TargetHeaderNotUnique => $"target_header_count_{BoundedCount(ObservedCount)}",
        CodexTargetSelectionFailure.TargetComposerNotUnique => $"composer_count_{BoundedCount(ObservedCount)}",
        CodexTargetSelectionFailure.TargetComposerValueUnavailable => "composer_value_unavailable",
        CodexTargetSelectionFailure.TargetComposerNotEmpty => "composer_not_empty",
        CodexTargetSelectionFailure.TargetDidNotChange => "target_not_changed",
        CodexTargetSelectionFailure.SelectionTimedOut => "selection_timed_out",
        _ => "selection_failed",
    };

    public static CodexTargetSelectionResult Selected(CodexComposerSession session) =>
        new(session ?? throw new ArgumentNullException(nameof(session)), null, null);

    public static CodexTargetSelectionResult Failed(
        CodexTargetSelectionFailure failure,
        int? observedCount = null) => new(null, failure, observedCount);

    private static string BoundedCount(int? count) => count switch
    {
        <= 0 => "0",
        1 => "1",
        _ => "many",
    };
}

public interface ICodexComposerAutomation
{
    Task<CodexTargetSelectionResult> SelectEmptyTargetAsync(
        string threadId,
        string expectedTitle,
        CodexTargetSelectionMode mode,
        CancellationToken cancellationToken);

    Task<bool> FocusAsync(
        CodexComposerSession session,
        CancellationToken cancellationToken);

    Task TypeAsync(
        CodexComposerSession session,
        string text,
        CancellationToken cancellationToken);

    Task<string> ReadTextAsync(
        CodexComposerSession session,
        CancellationToken cancellationToken);

    Task<bool> CanInvokeUniqueSendButtonAsync(
        CodexComposerSession session,
        CancellationToken cancellationToken);

    Task InvokeSendAsync(
        CodexComposerSession session,
        CancellationToken cancellationToken);

    Task<bool> ClearIfTextEqualsAsync(
        CodexComposerSession session,
        string expectedText,
        CancellationToken cancellationToken);
}
