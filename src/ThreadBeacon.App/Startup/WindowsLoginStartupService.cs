using Microsoft.Win32;
using System.IO;

namespace ThreadBeacon.App.Startup;

public interface IStartupRegistry
{
    string? Read();

    void Write(string value);

    void Delete();
}

public sealed class WindowsLoginStartupService : IDisposable
{
    private const string RunKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ThreadBeacon";

    private readonly string executablePath;
    private readonly IStartupRegistry registry;

    public WindowsLoginStartupService(
        string? executablePath = null,
        IStartupRegistry? registry = null)
    {
        this.executablePath = executablePath ?? Environment.ProcessPath
            ?? throw new InvalidOperationException("The current executable path is unavailable.");
        this.registry = registry ?? new CurrentUserStartupRegistry();
    }

    public bool IsEnabled
    {
        get
        {
            try
            {
                return string.Equals(
                    registry.Read(),
                    Quote(executablePath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception exception) when (IsRegistryFailure(exception))
            {
                return false;
            }
        }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            if (enabled)
            {
                registry.Write(Quote(executablePath));
            }
            else
            {
                registry.Delete();
            }
        }
        catch (Exception exception) when (IsRegistryFailure(exception))
        {
            // A blocked or unavailable user registry must not prevent the settings window from working.
        }
    }

    public void Dispose()
    {
        if (registry is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static string Quote(string path) => $"\"{path}\"";

    private static bool IsRegistryFailure(Exception exception) => exception is
        UnauthorizedAccessException or
        IOException or
        System.Security.SecurityException;

    private sealed class CurrentUserStartupRegistry : IStartupRegistry, IDisposable
    {
        private RegistryKey? key;

        public string? Read()
        {
            using RegistryKey? readOnlyKey = Registry.CurrentUser.OpenSubKey(
                RunKeyPath,
                writable: false);
            return readOnlyKey?.GetValue(ValueName) as string;
        }

        public void Write(string value)
        {
            key ??= Registry.CurrentUser.CreateSubKey(RunKeyPath);
            key.SetValue(ValueName, value, RegistryValueKind.String);
        }

        public void Delete()
        {
            using RegistryKey? writableKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            writableKey?.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        public void Dispose()
        {
            key?.Dispose();
            key = null;
        }

    }
}
