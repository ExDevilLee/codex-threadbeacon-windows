namespace ThreadBeacon.App.Windowing;

public sealed record WindowBounds(int X, int Y, int Width, int Height);

public sealed record DisplayArea(
    string Identifier,
    WindowBounds WorkingArea,
    bool IsPrimary);

public sealed record WindowPlacement(
    string DisplayIdentifier,
    WindowBounds Bounds);

public static class WindowPlacementResolver
{
    public static WindowPlacement? Resolve(
        WindowPlacement placement,
        IReadOnlyList<DisplayArea> displays)
    {
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(displays);

        DisplayArea[] validDisplays = displays
            .Where(display => display.WorkingArea.Width > 0
                && display.WorkingArea.Height > 0)
            .ToArray();
        if (validDisplays.Length == 0)
        {
            return null;
        }

        DisplayArea display = validDisplays.FirstOrDefault(candidate =>
                StringComparer.OrdinalIgnoreCase.Equals(
                    candidate.Identifier,
                    placement.DisplayIdentifier))
            ?? validDisplays.FirstOrDefault(candidate => candidate.IsPrimary)
            ?? validDisplays[0];
        WindowBounds workingArea = display.WorkingArea;
        int width = Math.Clamp(placement.Bounds.Width, 1, workingArea.Width);
        int height = Math.Clamp(placement.Bounds.Height, 1, workingArea.Height);
        long maximumX = (long)workingArea.X + workingArea.Width - width;
        long maximumY = (long)workingArea.Y + workingArea.Height - height;
        int x = (int)Math.Clamp((long)placement.Bounds.X, workingArea.X, maximumX);
        int y = (int)Math.Clamp((long)placement.Bounds.Y, workingArea.Y, maximumY);

        return new WindowPlacement(
            display.Identifier,
            new WindowBounds(x, y, width, height));
    }
}
