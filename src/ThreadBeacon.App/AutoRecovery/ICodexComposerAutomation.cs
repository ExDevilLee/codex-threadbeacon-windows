namespace ThreadBeacon.App.AutoRecovery;

public sealed record CodexComposerSession(string Id);

public interface ICodexComposerAutomation
{
    Task<CodexComposerSession?> SelectEmptyTargetAsync(
        string threadId,
        string expectedTitle,
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
