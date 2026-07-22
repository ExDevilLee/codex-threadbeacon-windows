using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ThreadBeacon.App.AutoRecovery;

public sealed class WindowsRecoveryForegroundSessionFactory : IRecoveryForegroundSessionFactory
{
    public IRecoveryForegroundSession Capture()
    {
        IntPtr originalWindow = NativeMethods.GetForegroundWindow();
        ForegroundApplicationIdentity? original = TryGetIdentity(originalWindow);
        ForegroundApplicationIdentity? codex = FindUniqueCodexIdentity();
        return new Session(originalWindow, original, codex);
    }

    private static ForegroundApplicationIdentity? FindUniqueCodexIdentity()
    {
        ForegroundApplicationIdentity[] identities = Process.GetProcessesByName("ChatGPT")
            .Select(TryGetCodexWindowIdentity)
            .Where(identity => identity is { IsCodex: true })
            .Cast<ForegroundApplicationIdentity>()
            .Distinct()
            .ToArray();
        return identities.Length == 1 ? identities[0] : null;
    }

    private static ForegroundApplicationIdentity? TryGetIdentity(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(windowHandle, out uint processId);
        return processId == 0 ? null : TryGetIdentity((int)processId);
    }

    private static ForegroundApplicationIdentity? TryGetCodexWindowIdentity(Process process)
    {
        using (process)
        {
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    return null;
                }

                ForegroundApplicationIdentity? identity = TryGetIdentity(process.Id);
                return identity is { IsCodex: true } ? identity : null;
            }
            catch
            {
                return null;
            }
        }
    }

    private static ForegroundApplicationIdentity? TryGetIdentity(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            bool isCodex = process.ProcessName.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase)
                && process.MainModule?.FileName.Contains(
                    "OpenAI.Codex_",
                    StringComparison.OrdinalIgnoreCase) == true;
            return new ForegroundApplicationIdentity(
                process.Id,
                process.StartTime.ToUniversalTime().Ticks,
                isCodex);
        }
        catch
        {
            return null;
        }
    }

    private sealed class Session(
        IntPtr originalWindow,
        ForegroundApplicationIdentity? originalApplication,
        ForegroundApplicationIdentity? codexApplication) : IRecoveryForegroundSession
    {
        public void RestoreIfSafe()
        {
            try
            {
                bool originalAvailable = NativeMethods.IsWindow(originalWindow)
                    && TryGetIdentity(originalWindow) == originalApplication;
                ForegroundApplicationIdentity? current = TryGetIdentity(
                    NativeMethods.GetForegroundWindow());
                if (ForegroundRestorationPolicy.Evaluate(
                        originalApplication,
                        codexApplication,
                        current,
                        originalAvailable) is ForegroundRestorationDecision.Restore)
                {
                    _ = NativeMethods.SetForegroundWindow(originalWindow);
                }
            }
            catch
            {
                // Foreground cleanup is best effort and never changes the send result.
            }
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindow(IntPtr windowHandle);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(
            IntPtr windowHandle,
            out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr windowHandle);
    }
}
