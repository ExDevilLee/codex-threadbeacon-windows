using ThreadBeacon.App.ViewModels;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class MonitoringStateTests
{
    [Fact]
    public void Constructor_DefaultsToActiveMonitoring()
    {
        var state = new MonitoringState();

        Assert.False(state.IsPaused);
        Assert.True(state.ShouldAutoRefresh);
    }

    [Fact]
    public void ToggleCommand_PausesThenResumesMonitoring()
    {
        var state = new MonitoringState();

        state.ToggleCommand.Execute(null);
        Assert.True(state.IsPaused);
        Assert.False(state.ShouldAutoRefresh);

        state.ToggleCommand.Execute(null);
        Assert.False(state.IsPaused);
        Assert.True(state.ShouldAutoRefresh);
    }
}
