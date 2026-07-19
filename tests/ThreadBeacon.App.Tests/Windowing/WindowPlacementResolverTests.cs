using ThreadBeacon.App.Windowing;

namespace ThreadBeacon.App.Tests.Windowing;

public sealed class WindowPlacementResolverTests
{
    [Fact]
    public void Resolve_PreservesVisiblePlacementOnSavedDisplay()
    {
        var placement = new WindowPlacement(
            "secondary",
            new WindowBounds(1600, 100, 620, 500));
        DisplayArea[] displays =
        [
            new("primary", new WindowBounds(0, 0, 1440, 900), true),
            new("secondary", new WindowBounds(1440, 0, 1280, 800), false),
        ];

        WindowPlacement? resolved = WindowPlacementResolver.Resolve(placement, displays);

        Assert.Equal(placement, resolved);
    }

    [Fact]
    public void Resolve_FallsBackToPrimaryAndClampsDisconnectedPlacement()
    {
        var placement = new WindowPlacement(
            "disconnected",
            new WindowBounds(3000, -500, 620, 500));
        DisplayArea[] displays =
        [
            new("secondary", new WindowBounds(-1280, 0, 1280, 720), false),
            new("primary", new WindowBounds(0, 40, 1440, 860), true),
        ];

        WindowPlacement? resolved = WindowPlacementResolver.Resolve(placement, displays);

        Assert.Equal(
            new WindowPlacement("primary", new WindowBounds(820, 40, 620, 500)),
            resolved);
    }

    [Fact]
    public void Resolve_FitsOversizedPlacementInsideWorkingArea()
    {
        var placement = new WindowPlacement(
            "small",
            new WindowBounds(0, 0, 900, 700));
        DisplayArea display = new(
            "small",
            new WindowBounds(100, 50, 480, 320),
            true);

        WindowPlacement? resolved = WindowPlacementResolver.Resolve(placement, [display]);

        Assert.Equal(
            new WindowPlacement("small", new WindowBounds(100, 50, 480, 320)),
            resolved);
    }

    [Fact]
    public void Resolve_WhenNoValidDisplayExists_ReturnsNull()
    {
        var placement = new WindowPlacement("missing", new WindowBounds(1, 2, 3, 4));

        Assert.Null(WindowPlacementResolver.Resolve(placement, []));
        Assert.Null(WindowPlacementResolver.Resolve(
            placement,
            [new DisplayArea("invalid", new WindowBounds(0, 0, 0, 500), true)]));
    }
}
