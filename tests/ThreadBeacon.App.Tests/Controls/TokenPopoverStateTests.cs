using ThreadBeacon.App.Controls;

namespace ThreadBeacon.App.Tests.Controls;

public sealed class TokenPopoverStateTests
{
    [Fact]
    public void OpenForHover_OpensWithoutPinning()
    {
        var state = new TokenPopoverState();

        state.OpenForHover();

        Assert.True(state.IsOpen);
        Assert.False(state.IsPinned);
    }

    [Fact]
    public void TogglePinned_OpensAndPinsThenClosesAndUnpins()
    {
        var state = new TokenPopoverState();

        state.TogglePinned();
        Assert.True(state.IsOpen);
        Assert.True(state.IsPinned);

        state.TogglePinned();
        Assert.False(state.IsOpen);
        Assert.False(state.IsPinned);
    }

    [Fact]
    public void RequestHoverDismiss_WhenPinned_KeepsOpen()
    {
        var state = new TokenPopoverState();
        state.TogglePinned();

        state.RequestHoverDismiss();

        Assert.True(state.IsOpen);
        Assert.True(state.IsPinned);
    }

    [Fact]
    public void RequestHoverDismiss_WhenNotPinned_Closes()
    {
        var state = new TokenPopoverState();
        state.OpenForHover();

        state.RequestHoverDismiss();

        Assert.False(state.IsOpen);
        Assert.False(state.IsPinned);
    }

    [Fact]
    public void Close_ResetsOpenAndPinnedState()
    {
        var state = new TokenPopoverState();
        state.TogglePinned();

        state.Close();

        Assert.False(state.IsOpen);
        Assert.False(state.IsPinned);
    }
}
