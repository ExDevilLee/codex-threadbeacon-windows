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
            CodexComposerSession? session = await automation.SelectEmptyTargetAsync(
                threadId,
                expectedTitle,
                cancellationToken).ConfigureAwait(false);
            return session is not null
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
