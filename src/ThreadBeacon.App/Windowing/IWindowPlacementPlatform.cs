namespace ThreadBeacon.App.Windowing;

public interface IWindowPlacementPlatform
{
    IReadOnlyList<DisplayArea> GetDisplays();

    WindowPlacement? Capture(nint windowHandle);

    bool Apply(nint windowHandle, WindowBounds bounds);
}
