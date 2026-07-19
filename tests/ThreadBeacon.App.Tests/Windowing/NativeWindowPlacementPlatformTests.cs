using ThreadBeacon.App.Windowing;

namespace ThreadBeacon.App.Tests.Windowing;

public sealed class NativeWindowPlacementPlatformTests
{
    [Fact]
    public void GetDisplays_ReturnsValidWindowsWorkingAreas()
    {
        var platform = new NativeWindowPlacementPlatform();

        IReadOnlyList<DisplayArea> displays = platform.GetDisplays();

        Assert.NotEmpty(displays);
        Assert.Contains(displays, display => display.IsPrimary);
        Assert.All(displays, display =>
        {
            Assert.StartsWith(@"\\.\DISPLAY", display.Identifier, StringComparison.OrdinalIgnoreCase);
            Assert.True(display.WorkingArea.Width > 0);
            Assert.True(display.WorkingArea.Height > 0);
        });
    }
}
