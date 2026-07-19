namespace ThreadBeacon.App.Windowing;

public sealed class WindowPlacementCoordinator
{
    private readonly IWindowPlacementStore store;
    private readonly IWindowPlacementPlatform platform;

    public WindowPlacementCoordinator(
        IWindowPlacementStore store,
        IWindowPlacementPlatform platform)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.platform = platform ?? throw new ArgumentNullException(nameof(platform));
    }

    public bool Restore(nint windowHandle)
    {
        try
        {
            WindowPlacement? saved = store.Load();
            if (saved is null)
            {
                return false;
            }

            WindowPlacement? resolved = WindowPlacementResolver.Resolve(
                saved,
                platform.GetDisplays());
            if (resolved is null || !platform.Apply(windowHandle, resolved.Bounds))
            {
                return false;
            }

            store.Save(resolved);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Capture(nint windowHandle)
    {
        try
        {
            WindowPlacement? placement = platform.Capture(windowHandle);
            return placement is not null && store.Save(placement);
        }
        catch
        {
            return false;
        }
    }
}
