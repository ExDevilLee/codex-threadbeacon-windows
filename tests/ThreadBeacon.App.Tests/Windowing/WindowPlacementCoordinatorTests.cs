using ThreadBeacon.App.Windowing;

namespace ThreadBeacon.App.Tests.Windowing;

public sealed class WindowPlacementCoordinatorTests
{
    [Fact]
    public void Restore_ResolvesAppliesAndRewritesPlacement()
    {
        var store = new MemoryPlacementStore
        {
            Current = new WindowPlacement(
                "disconnected",
                new WindowBounds(3000, -200, 620, 500)),
        };
        var platform = new FakePlacementPlatform
        {
            Displays =
            [
                new DisplayArea(
                    "primary",
                    new WindowBounds(0, 40, 1440, 860),
                    true),
            ],
        };
        var coordinator = new WindowPlacementCoordinator(store, platform);

        bool restored = coordinator.Restore((nint)42);

        var expected = new WindowPlacement(
            "primary",
            new WindowBounds(820, 40, 620, 500));
        Assert.True(restored);
        Assert.Equal(expected.Bounds, platform.AppliedBounds);
        Assert.Equal(expected, store.Current);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public void Restore_WithoutSavedPlacement_DoesNotQueryOrApplyPlatform()
    {
        var platform = new FakePlacementPlatform();
        var coordinator = new WindowPlacementCoordinator(
            new MemoryPlacementStore(),
            platform);

        bool restored = coordinator.Restore((nint)42);

        Assert.False(restored);
        Assert.Equal(0, platform.DisplayQueryCount);
        Assert.Null(platform.AppliedBounds);
    }

    [Fact]
    public void Capture_PersistsPlatformPlacement()
    {
        var captured = new WindowPlacement(
            @"\\.\DISPLAY2",
            new WindowBounds(-620, 80, 620, 500));
        var store = new MemoryPlacementStore();
        var platform = new FakePlacementPlatform { Captured = captured };
        var coordinator = new WindowPlacementCoordinator(store, platform);

        bool saved = coordinator.Capture((nint)42);

        Assert.True(saved);
        Assert.Equal(captured, store.Current);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public void NativeFailure_DoesNotEscape()
    {
        var store = new MemoryPlacementStore
        {
            Current = new WindowPlacement("display", new WindowBounds(0, 0, 620, 500)),
        };
        var platform = new FakePlacementPlatform { ThrowOnAccess = true };
        var coordinator = new WindowPlacementCoordinator(store, platform);

        Exception? restoreFailure = Record.Exception(() => coordinator.Restore((nint)42));
        Exception? captureFailure = Record.Exception(() => coordinator.Capture((nint)42));

        Assert.Null(restoreFailure);
        Assert.Null(captureFailure);
    }

    private sealed class MemoryPlacementStore : IWindowPlacementStore
    {
        public WindowPlacement? Current { get; set; }

        public int SaveCount { get; private set; }

        public WindowPlacement? Load() => Current;

        public bool Save(WindowPlacement placement)
        {
            Current = placement;
            SaveCount++;
            return true;
        }
    }

    private sealed class FakePlacementPlatform : IWindowPlacementPlatform
    {
        public IReadOnlyList<DisplayArea> Displays { get; init; } = [];

        public WindowPlacement? Captured { get; init; }

        public bool ThrowOnAccess { get; init; }

        public int DisplayQueryCount { get; private set; }

        public WindowBounds? AppliedBounds { get; private set; }

        public IReadOnlyList<DisplayArea> GetDisplays()
        {
            if (ThrowOnAccess)
            {
                throw new InvalidOperationException("Native display query failed.");
            }

            DisplayQueryCount++;
            return Displays;
        }

        public WindowPlacement? Capture(nint windowHandle)
        {
            if (ThrowOnAccess)
            {
                throw new InvalidOperationException("Native capture failed.");
            }

            return Captured;
        }

        public bool Apply(nint windowHandle, WindowBounds bounds)
        {
            if (ThrowOnAccess)
            {
                throw new InvalidOperationException("Native apply failed.");
            }

            AppliedBounds = bounds;
            return true;
        }
    }
}
