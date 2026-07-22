using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.AutoRecovery;

public sealed class WindowsCodexRecoverySender : IAutoRecoverySender
{
    private readonly ICodexComposerAutomation automation;
    private readonly IRolloutRecoveryEvidenceMonitor evidenceMonitor;
    private readonly IRecoveryForegroundSessionFactory foregroundSessionFactory;
    private readonly SemaphoreSlim sendGate = new(1, 1);

    public WindowsCodexRecoverySender(
        ICodexComposerAutomation automation,
        IRolloutRecoveryEvidenceMonitor evidenceMonitor,
        IRecoveryForegroundSessionFactory? foregroundSessionFactory = null)
    {
        this.automation = automation ?? throw new ArgumentNullException(nameof(automation));
        this.evidenceMonitor = evidenceMonitor
            ?? throw new ArgumentNullException(nameof(evidenceMonitor));
        this.foregroundSessionFactory = foregroundSessionFactory
            ?? NoOpRecoveryForegroundSessionFactory.Instance;
    }

    public async Task<AutoRecoverySendResult> SendAsync(
        AutoRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        IRecoveryForegroundSession foregroundSession;
        try
        {
            foregroundSession = foregroundSessionFactory.Capture();
        }
        catch
        {
            foregroundSession = NoOpRecoveryForegroundSession.Instance;
        }

        try
        {
            return await SendCoreAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return AutoRecoverySendResult.Failed(
                AutoRecoveryDiagnosticCodes.UnexpectedError);
        }
        finally
        {
            try
            {
                foregroundSession.RestoreIfSafe();
            }
            catch
            {
                // Foreground cleanup is best effort and never changes the send result.
            }

            sendGate.Release();
        }
    }

    private async Task<AutoRecoverySendResult> SendCoreAsync(
        AutoRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        RolloutRecoveryCheckpoint checkpoint = evidenceMonitor.Capture(
            request.Candidate.RolloutPath);
        CodexTargetSelectionResult selection = await automation.SelectEmptyTargetAsync(
            request.Candidate.ThreadId,
            request.Candidate.Title,
            CodexTargetSelectionMode.Unattended,
            cancellationToken).ConfigureAwait(false);
        if (!selection.IsSelected)
        {
            return AutoRecoverySendResult.Failed(selection.DiagnosticCode);
        }

        CodexComposerSession session = selection.Session!;
        if (!await automation.FocusAsync(session, cancellationToken).ConfigureAwait(false))
        {
            return AutoRecoverySendResult.Failed("composer_focus_failed");
        }

        await automation.TypeAsync(session, request.Prompt, cancellationToken).ConfigureAwait(false);
        string readback = await automation.ReadTextAsync(session, cancellationToken).ConfigureAwait(false);
        if (!MatchesPrompt(readback, request.Prompt))
        {
            return AutoRecoverySendResult.Failed("composer_readback_mismatch");
        }

        if (!await automation.CanInvokeUniqueSendButtonAsync(
                session,
                cancellationToken).ConfigureAwait(false))
        {
            await automation.ClearIfTextEqualsAsync(
                session,
                request.Prompt,
                cancellationToken).ConfigureAwait(false);
            return AutoRecoverySendResult.Failed("send_button_unavailable");
        }

        string finalReadback = await automation.ReadTextAsync(
            session,
            cancellationToken).ConfigureAwait(false);
        if (!MatchesPrompt(finalReadback, request.Prompt))
        {
            return AutoRecoverySendResult.Failed("composer_changed_before_send");
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
            : AutoRecoverySendResult.Failed("rollout_evidence_missing");
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
