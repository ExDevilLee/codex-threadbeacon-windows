using ThreadBeacon.App.Settings;
using ThreadBeacon.App.ViewModels;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class WindowPinStateTests
{
    [Fact]
    public void Constructor_RestoresPinnedState()
    {
        var state = new WindowPinState(
            new FakeSettingsStore(new AppSettings { IsWindowPinned = true }));

        Assert.True(state.IsPinned);
    }

    [Fact]
    public void ToggleCommand_TogglesAndSavesState()
    {
        var store = new FakeSettingsStore(new AppSettings());
        var state = new WindowPinState(store);

        state.ToggleCommand.Execute(null);

        Assert.True(state.IsPinned);
        Assert.NotNull(store.LastSaved);
        Assert.True(store.LastSaved.IsWindowPinned);
    }

    [Fact]
    public void ToggleCommand_WhenSaveFails_KeepsCurrentState()
    {
        var store = new FakeSettingsStore(new AppSettings(), saveResult: false);
        var state = new WindowPinState(store);

        state.ToggleCommand.Execute(null);

        Assert.True(state.IsPinned);
        Assert.NotNull(store.LastSaved);
        Assert.True(store.LastSaved.IsWindowPinned);
    }

    private sealed class FakeSettingsStore(AppSettings initial, bool saveResult = true)
        : IAppSettingsStore
    {
        public AppSettings? LastSaved { get; private set; }

        public AppSettings Load() => initial;

        public bool Save(AppSettings settings)
        {
            LastSaved = settings;
            return saveResult;
        }
    }
}
