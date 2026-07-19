using System.Runtime.InteropServices;

namespace ThreadBeacon.App.Windowing;

public sealed class NativeWindowPlacementPlatform : IWindowPlacementPlatform
{
    private const uint MonitorDefaultToNearest = 2;
    private const uint MonitorInfoPrimary = 1;
    private const uint SetWindowPositionFlags = 0x0004 | 0x0010 | 0x0200;

    public IReadOnlyList<DisplayArea> GetDisplays()
    {
        try
        {
            var displays = new List<DisplayArea>();
            NativeMethods.MonitorEnumProc callback = (
                monitor,
                _,
                _,
                _) =>
            {
                if (TryGetMonitorInfo(monitor, out MonitorInfoEx info))
                {
                    displays.Add(new DisplayArea(
                        info.DeviceName,
                        ToBounds(info.WorkArea),
                        (info.Flags & MonitorInfoPrimary) != 0));
                }

                return true;
            };

            return NativeMethods.EnumDisplayMonitors(0, 0, callback, 0)
                ? displays
                : [];
        }
        catch
        {
            return [];
        }
    }

    public WindowPlacement? Capture(nint windowHandle)
    {
        try
        {
            if (windowHandle == 0
                || !NativeMethods.GetWindowRect(windowHandle, out NativeRect windowRect))
            {
                return null;
            }

            nint monitor = NativeMethods.MonitorFromWindow(
                windowHandle,
                MonitorDefaultToNearest);
            if (monitor == 0 || !TryGetMonitorInfo(monitor, out MonitorInfoEx info))
            {
                return null;
            }

            WindowBounds bounds = ToBounds(windowRect);
            return bounds.Width > 0 && bounds.Height > 0
                ? new WindowPlacement(info.DeviceName, bounds)
                : null;
        }
        catch
        {
            return null;
        }
    }

    public bool Apply(nint windowHandle, WindowBounds bounds)
    {
        try
        {
            return windowHandle != 0
                && bounds.Width > 0
                && bounds.Height > 0
                && NativeMethods.SetWindowPos(
                    windowHandle,
                    0,
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    bounds.Height,
                    SetWindowPositionFlags);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetMonitorInfo(nint monitor, out MonitorInfoEx info)
    {
        info = new MonitorInfoEx
        {
            Size = Marshal.SizeOf<MonitorInfoEx>(),
            DeviceName = string.Empty,
        };
        return NativeMethods.GetMonitorInfo(monitor, ref info)
            && !string.IsNullOrWhiteSpace(info.DeviceName);
    }

    private static WindowBounds ToBounds(NativeRect rectangle) => new(
        rectangle.Left,
        rectangle.Top,
        rectangle.Right - rectangle.Left,
        rectangle.Bottom - rectangle.Top);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    private static class NativeMethods
    {
        internal delegate bool MonitorEnumProc(
            nint monitor,
            nint deviceContext,
            nint monitorRectangle,
            nint data);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumDisplayMonitors(
            nint deviceContext,
            nint clipRectangle,
            MonitorEnumProc callback,
            nint data);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(
            nint monitor,
            ref MonitorInfoEx monitorInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(
            nint windowHandle,
            out NativeRect rectangle);

        [DllImport("user32.dll")]
        internal static extern nint MonitorFromWindow(
            nint windowHandle,
            uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(
            nint windowHandle,
            nint insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);
    }
}
