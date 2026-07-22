namespace ThreadBeacon.App.AutoRecovery;

public interface ICodexThreadOpener
{
    Task<bool> OpenAsync(
        string threadId,
        string expectedTitle,
        CancellationToken cancellationToken = default);
}

public sealed class WindowsCodexThreadOpener : ICodexThreadOpener
{
    private readonly ICodexComposerAutomation automation;

    public WindowsCodexThreadOpener(ICodexComposerAutomation automation)
    {
        this.automation = automation ?? throw new ArgumentNullException(nameof(automation));
    }

    public async Task<bool> OpenAsync(
        string threadId,
        string expectedTitle,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(threadId, out _) || string.IsNullOrWhiteSpace(expectedTitle))
        {
            return false;
        }

        try
        {
            CodexTargetSelectionResult selection = await automation.SelectEmptyTargetAsync(
                threadId,
                expectedTitle,
                CodexTargetSelectionMode.Interactive,
                cancellationToken).ConfigureAwait(false);
            return selection.Session is CodexComposerSession session
                && await automation.FocusAsync(session, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }
}
