using ThreadBeacon.App.AutoRecovery;
using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.Tests.AutoRecovery;

public sealed class WindowsCodexRecoverySenderTests
{
    [Fact]
    public async Task SendAsync_StopsBeforeTypingWhenTargetCannotBeSelected()
    {
        var automation = new FakeCodexComposerAutomation { Selection = null };
        var sender = new WindowsCodexRecoverySender(automation, new FakeEvidenceMonitor());

        AutoRecoverySendResult result = await sender.SendAsync(Request(), default);

        Assert.Equal(AutoRecoverySendStatus.Failed, result.Status);
        Assert.Equal(0, automation.TypeCount);
        Assert.Equal(0, automation.InvokeCount);
    }

    [Fact]
    public async Task SendAsync_ClearsVerifiedPromptWhenSendButtonIsUnavailable()
    {
        var automation = new FakeCodexComposerAutomation { CanInvoke = false };
        var sender = new WindowsCodexRecoverySender(automation, new FakeEvidenceMonitor());

        AutoRecoverySendResult result = await sender.SendAsync(Request(), default);

        Assert.Equal(AutoRecoverySendStatus.Failed, result.Status);
        Assert.Equal(1, automation.ClearCount);
        Assert.Equal(0, automation.InvokeCount);
    }

    [Fact]
    public async Task SendAsync_DoesNotClearWhenReadbackDiffersFromPrompt()
    {
        var automation = new FakeCodexComposerAutomation { Readback = "user draft" };
        var sender = new WindowsCodexRecoverySender(automation, new FakeEvidenceMonitor());

        AutoRecoverySendResult result = await sender.SendAsync(Request(), default);

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

        AutoRecoverySendResult result = await sender.SendAsync(Request(), default);

        Assert.Equal(AutoRecoverySendStatus.Sent, result.Status);
        Assert.Equal(1, automation.InvokeCount);
        Assert.Equal(1, evidence.WaitCount);
    }

    [Fact]
    public async Task SendAsync_NeverRetriesAfterInvokeWhenEvidenceTimesOut()
    {
        var automation = new FakeCodexComposerAutomation();
        var sender = new WindowsCodexRecoverySender(automation, new FakeEvidenceMonitor());

        AutoRecoverySendResult result = await sender.SendAsync(Request(), default);

        Assert.Equal(AutoRecoverySendStatus.Failed, result.Status);
        Assert.Equal(1, automation.InvokeCount);
        Assert.Equal(0, automation.ClearCount);
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
    public CodexComposerSession? Selection { get; init; } = new("session-1");
    public bool CanInvoke { get; init; } = true;
    public string? Readback { get; init; }
    public int TypeCount { get; private set; }
    public int ClearCount { get; private set; }
    public int InvokeCount { get; private set; }

    public Task<CodexComposerSession?> SelectEmptyTargetAsync(
        string threadId,
        string expectedTitle,
        CancellationToken cancellationToken) => Task.FromResult(Selection);

    public Task<bool> FocusAsync(CodexComposerSession session, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task TypeAsync(CodexComposerSession session, string text, CancellationToken cancellationToken)
    {
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
