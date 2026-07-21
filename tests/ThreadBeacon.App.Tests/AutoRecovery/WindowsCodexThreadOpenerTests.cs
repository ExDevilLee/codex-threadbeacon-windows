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
            "019f84ea-dbd8-76e3-b1d6-580452c420ce",
            "Title",
            default);

        Assert.True(opened);
        Assert.Equal(0, automation.TypeCount);
        Assert.Equal(0, automation.InvokeCount);
    }

    [Fact]
    public async Task OpenAsync_ReturnsFalseWhenTargetSelectionFails()
    {
        var automation = new FakeCodexComposerAutomation { Selection = null };
        var opener = new WindowsCodexThreadOpener(automation);

        Assert.False(await opener.OpenAsync(
            "019f84ea-dbd8-76e3-b1d6-580452c420ce",
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
