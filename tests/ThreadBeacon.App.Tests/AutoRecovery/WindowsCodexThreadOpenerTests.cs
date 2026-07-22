using ThreadBeacon.App.AutoRecovery;

namespace ThreadBeacon.App.Tests.AutoRecovery;

public sealed class WindowsCodexThreadOpenerTests
{
    [Fact]
    public async Task OpenAsync_SelectsAndFocusesWithoutTypingOrSending()
    {
        var automation = new FakeCodexComposerAutomation();
        var opener = new WindowsCodexThreadOpener(automation);

        bool opened = await opener.OpenAsync(
            "11111111-2222-4333-8444-555555555555",
            "Title",
            default);

        Assert.True(opened);
        Assert.Equal(CodexTargetSelectionMode.Interactive, automation.SelectionMode);
        Assert.Equal(0, automation.TypeCount);
        Assert.Equal(0, automation.InvokeCount);
    }

    [Fact]
    public async Task OpenAsync_ReturnsFalseWhenTargetSelectionFails()
    {
        var automation = new FakeCodexComposerAutomation
        {
            Selection = CodexTargetSelectionResult.Failed(
                CodexTargetSelectionFailure.DeepLinkFailed),
        };
        var opener = new WindowsCodexThreadOpener(automation);

        Assert.False(await opener.OpenAsync(
            "11111111-2222-4333-8444-555555555555",
            "Title",
            default));
    }

    [Fact]
    public async Task OpenAsync_RejectsInvalidThreadId()
    {
        var automation = new FakeCodexComposerAutomation();
        var opener = new WindowsCodexThreadOpener(automation);

        Assert.False(await opener.OpenAsync("not-a-guid", "Title", default));
        Assert.Equal(0, automation.TypeCount);
        Assert.Equal(0, automation.InvokeCount);
    }
}
