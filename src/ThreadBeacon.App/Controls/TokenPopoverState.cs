namespace ThreadBeacon.App.Controls;

public sealed class TokenPopoverState
{
    public bool IsOpen { get; private set; }

    public bool IsPinned { get; private set; }

    public void OpenForHover() => IsOpen = true;

    public void TogglePinned()
    {
        IsPinned = !IsPinned;
        IsOpen = IsPinned;
    }

    public void RequestHoverDismiss()
    {
        if (!IsPinned)
        {
            IsOpen = false;
        }
    }

    public void Close()
    {
        IsOpen = false;
        IsPinned = false;
    }
}
