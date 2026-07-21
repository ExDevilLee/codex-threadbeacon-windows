using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.AutoRecovery;

public sealed class WindowsCodexRecoverySender : IAutoRecoverySender
{
    private readonly ICodexComposerAutomation automation;
    private readonly IRolloutRecoveryEvidenceMonitor evidenceMonitor;
    private readonly SemaphoreSlim sendGate = new(1, 1);

    public WindowsCodexRecoverySender(
        ICodexComposerAutomation automation,
        IRolloutRecoveryEvidenceMonitor evidenceMonitor)
    {
        this.automation = automation ?? throw new ArgumentNullException(nameof(automation));
        this.evidenceMonitor = evidenceMonitor
            ?? throw new ArgumentNullException(nameof(evidenceMonitor));
    }

    public async Task<AutoRecoverySendResult> SendAsync(
        AutoRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await SendCoreAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return AutoRecoverySendResult.Failed(exception.Message);
        }
        finally
        {
            sendGate.Release();
        }
    }

    private async Task<AutoRecoverySendResult> SendCoreAsync(
        AutoRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        RolloutRecoveryCheckpoint checkpoint = evidenceMonitor.Capture(
            request.Candidate.RolloutPath);
        CodexComposerSession? session = await automation.SelectEmptyTargetAsync(
            request.Candidate.ThreadId,
            request.Candidate.Title,
            cancellationToken).ConfigureAwait(false);
        if (session is null
            || !await automation.FocusAsync(session, cancellationToken).ConfigureAwait(false))
        {
            return AutoRecoverySendResult.Failed("Codex target was not safely selectable.");
        }

        await automation.TypeAsync(session, request.Prompt, cancellationToken).ConfigureAwait(false);
        string readback = await automation.ReadTextAsync(session, cancellationToken).ConfigureAwait(false);
        if (!MatchesPrompt(readback, request.Prompt))
        {
            return AutoRecoverySendResult.Failed("Composer readback did not match the recovery prompt.");
        }

        if (!await automation.CanInvokeUniqueSendButtonAsync(
                session,
                cancellationToken).ConfigureAwait(false))
        {
            await automation.ClearIfTextEqualsAsync(
                session,
                request.Prompt,
                cancellationToken).ConfigureAwait(false);
            return AutoRecoverySendResult.Failed("A unique send button was not available.");
        }

        string finalReadback = await automation.ReadTextAsync(
            session,
            cancellationToken).ConfigureAwait(false);
        if (!MatchesPrompt(finalReadback, request.Prompt))
        {
            return AutoRecoverySendResult.Failed("Composer changed before sending.");
        }

        // Invoke exactly once. Evidence timeout is reported as failure and is never retried.
        await automation.InvokeSendAsync(session, cancellationToken).ConfigureAwait(false);
        bool verified = await evidenceMonitor.WaitForEvidenceAsync(
            request.Candidate.RolloutPath,
            checkpoint,
            request.Prompt,
            cancellationToken).ConfigureAwait(false);
        return verified
            ? AutoRecoverySendResult.Sent
            : AutoRecoverySendResult.Failed("Send was invoked but rollout evidence was not observed.");
    }

    private static bool MatchesPrompt(string actual, string expected)
    {
        if (!actual.StartsWith(expected, StringComparison.Ordinal))
        {
            return false;
        }

        ReadOnlySpan<char> suffix = actual.AsSpan(expected.Length);
        return suffix.Length <= 2 && suffix.IndexOfAnyExcept('\r', '\n') < 0;
    }
}
