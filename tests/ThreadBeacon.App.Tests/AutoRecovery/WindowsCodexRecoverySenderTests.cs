using ThreadBeacon.App.AutoRecovery;
using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.Tests.AutoRecovery;

public sealed class WindowsCodexRecoverySenderTests
{
    [Fact]
    public async Task SendAsync_StopsBeforeTypingWhenTargetCannotBeSelected()
    {
        var automation = new FakeCodexComposerAutomation
        {
            Selection = CodexTargetSelectionResult.Failed(
                CodexTargetSelectionFailure.CodexForeground),
        };
        var sender = new WindowsCodexRecoverySender(automation, new FakeEvidenceMonitor());
        int automationStartCount = 0;

        AutoRecoverySendResult result = await sender.SendAsync(
            Request(),
            () => automationStartCount++,
            default);

        Assert.Equal(AutoRecoverySendStatus.Failed, result.Status);
        Assert.Equal("codex_frontmost", result.DiagnosticCode);
        Assert.Equal(CodexTargetSelectionMode.Unattended, automation.SelectionMode);
        Assert.Equal(0, automationStartCount);
        Assert.Equal(0, automation.TypeCount);
        Assert.Equal(0, automation.InvokeCount);
    }

    [Fact]
    public async Task SendAsync_DoesNotStartAutomationWhenComposerCannotBeFocused()
    {
        var automation = new FakeCodexComposerAutomation { CanFocus = false };
        var sender = new WindowsCodexRecoverySender(automation, new FakeEvidenceMonitor());
        int automationStartCount = 0;

        AutoRecoverySendResult result = await sender.SendAsync(
            Request(),
            () => automationStartCount++,
            default);

        Assert.Equal(AutoRecoverySendStatus.Failed, result.Status);
        Assert.Equal("composer_focus_failed", result.DiagnosticCode);
        Assert.Equal(0, automationStartCount);
        Assert.Equal(0, automation.TypeCount);
    }

    [Fact]
    public async Task SendAsync_ClearsVerifiedPromptWhenSendButtonIsUnavailable()
    {
        var automation = new FakeCodexComposerAutomation { CanInvoke = false };
        var sender = new WindowsCodexRecoverySender(automation, new FakeEvidenceMonitor());

        AutoRecoverySendResult result = await sender.SendAsync(Request(), () => { }, default);

        Assert.Equal(AutoRecoverySendStatus.Failed, result.Status);
        Assert.Equal(1, automation.ClearCount);
        Assert.Equal(0, automation.InvokeCount);
    }

    [Fact]
    public async Task SendAsync_DoesNotClearWhenReadbackDiffersFromPrompt()
    {
        var automation = new FakeCodexComposerAutomation { Readback = "user draft" };
        var sender = new WindowsCodexRecoverySender(automation, new FakeEvidenceMonitor());

        AutoRecoverySendResult result = await sender.SendAsync(Request(), () => { }, default);

        Assert.Equal(AutoRecoverySendStatus.Failed, result.Status);
        Assert.Equal(0, automation.ClearCount);
        Assert.Equal(0, automation.InvokeCount);
    }

    [Fact]
    public async Task SendAsync_InvokesOnceAndReturnsSentAfterRolloutEvidence()
    {
        var automation = new FakeCodexComposerAutomation();
        var evidence = new FakeEvidenceMonitor { IsVerified = true };
        var sender = new WindowsCodexRecoverySender(automation, evidence);
        int automationStartCount = 0;

        AutoRecoverySendResult result = await sender.SendAsync(
            Request(),
            () => automationStartCount++,
            default);

        Assert.Equal(AutoRecoverySendStatus.Sent, result.Status);
        Assert.Equal(1, automationStartCount);
        Assert.Equal(1, automation.InvokeCount);
        Assert.Equal(1, evidence.WaitCount);
    }

    [Fact]
    public async Task SendAsync_NeverRetriesAfterInvokeWhenEvidenceTimesOut()
    {
        var automation = new FakeCodexComposerAutomation();
        var sender = new WindowsCodexRecoverySender(automation, new FakeEvidenceMonitor());

        AutoRecoverySendResult result = await sender.SendAsync(Request(), () => { }, default);

        Assert.Equal(AutoRecoverySendStatus.Failed, result.Status);
        Assert.Equal(1, automation.InvokeCount);
        Assert.Equal(0, automation.ClearCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendAsync_RestoresForegroundAfterSuccessOrFailure(bool verified)
    {
        var foreground = new FakeRecoveryForegroundSessionFactory();
        var sender = new WindowsCodexRecoverySender(
            new FakeCodexComposerAutomation(),
            new FakeEvidenceMonitor { IsVerified = verified },
            foreground);

        await sender.SendAsync(Request(), () => { }, default);

        Assert.Equal(1, foreground.CaptureCount);
        Assert.Equal(1, foreground.Session.RestoreCount);
    }

    [Fact]
    public async Task SendAsync_RestoresForegroundWhenCancellationEscapes()
    {
        var foreground = new FakeRecoveryForegroundSessionFactory();
        using var cancellation = new CancellationTokenSource();
        var automation = new FakeCodexComposerAutomation { CancelOnType = cancellation };
        var sender = new WindowsCodexRecoverySender(
            automation,
            new FakeEvidenceMonitor(),
            foreground);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sender.SendAsync(Request(), () => { }, cancellation.Token));

        Assert.Equal(1, foreground.Session.RestoreCount);
    }

    private static AutoRecoveryRequest Request() => new(
        new AutoRecoveryCandidate(
            "thread-1",
            "episode-1",
            AutoRecoveryIncidentType.Http400,
            "Renamed title",
            @"C:\Codex\rollout.jsonl",
            DateTimeOffset.UnixEpoch),
        "Continue safely.");
}

internal sealed class FakeCodexComposerAutomation : ICodexComposerAutomation
{
    public CodexTargetSelectionResult Selection { get; init; } =
        CodexTargetSelectionResult.Selected(new CodexComposerSession("session-1"));
    public bool CanInvoke { get; init; } = true;
    public bool CanFocus { get; init; } = true;
    public string? Readback { get; init; }
    public int TypeCount { get; private set; }
    public int ClearCount { get; private set; }
    public int InvokeCount { get; private set; }
    public CancellationTokenSource? CancelOnType { get; init; }
    public CodexTargetSelectionMode? SelectionMode { get; private set; }

    public Task<CodexTargetSelectionResult> SelectEmptyTargetAsync(
        string threadId,
        string expectedTitle,
        CodexTargetSelectionMode mode,
        CancellationToken cancellationToken)
    {
        SelectionMode = mode;
        return Task.FromResult(Selection);
    }

    public Task<bool> FocusAsync(CodexComposerSession session, CancellationToken cancellationToken) =>
        Task.FromResult(CanFocus);

    public Task TypeAsync(CodexComposerSession session, string text, CancellationToken cancellationToken)
    {
        if (CancelOnType is not null)
        {
            CancelOnType.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
        }

        TypeCount++;
        return Task.CompletedTask;
    }

    public Task<string> ReadTextAsync(CodexComposerSession session, CancellationToken cancellationToken) =>
        Task.FromResult(Readback ?? "Continue safely.");

    public Task<bool> CanInvokeUniqueSendButtonAsync(
        CodexComposerSession session,
        CancellationToken cancellationToken) => Task.FromResult(CanInvoke);

    public Task InvokeSendAsync(CodexComposerSession session, CancellationToken cancellationToken)
    {
        InvokeCount++;
        return Task.CompletedTask;
    }

    public Task<bool> ClearIfTextEqualsAsync(
        CodexComposerSession session,
        string expectedText,
        CancellationToken cancellationToken)
    {
        ClearCount++;
        return Task.FromResult(true);
    }
}

internal sealed class FakeRecoveryForegroundSessionFactory : IRecoveryForegroundSessionFactory
{
    public int CaptureCount { get; private set; }

    public FakeRecoveryForegroundSession Session { get; } = new();

    public IRecoveryForegroundSession Capture()
    {
        CaptureCount++;
        return Session;
    }
}

internal sealed class FakeRecoveryForegroundSession : IRecoveryForegroundSession
{
    public int RestoreCount { get; private set; }

    public void RestoreIfSafe() => RestoreCount++;
}

internal sealed class FakeEvidenceMonitor : IRolloutRecoveryEvidenceMonitor
{
    public bool IsVerified { get; init; }
    public int WaitCount { get; private set; }

    public RolloutRecoveryCheckpoint Capture(string rolloutPath) => new(0);

    public Task<bool> WaitForEvidenceAsync(
        string rolloutPath,
        RolloutRecoveryCheckpoint checkpoint,
        string expectedMessage,
        CancellationToken cancellationToken)
    {
        WaitCount++;
        return Task.FromResult(IsVerified);
    }
}
