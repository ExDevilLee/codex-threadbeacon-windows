using Microsoft.Win32;
using System.IO;

namespace ThreadBeacon.App.Theme;

public interface IAppThemeDetector : IDisposable
{
    event EventHandler? Changed;

    AppTheme ReadCurrentTheme();
}

public sealed class WindowsSystemThemeDetector : IAppThemeDetector
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private bool isDisposed;

    public WindowsSystemThemeDetector()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public event EventHandler? Changed;

    public AppTheme ReadCurrentTheme()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return ResolveRegistryValue(key?.GetValue("AppsUseLightTheme"));
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException
            or IOException
            or System.Security.SecurityException)
        {
            return AppTheme.Light;
        }
    }

    public static AppTheme ResolveRegistryValue(object? value) => value switch
    {
        0 => AppTheme.Dark,
        1 => AppTheme.Light,
        _ => AppTheme.Light,
    };

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) =>
        Changed?.Invoke(this, EventArgs.Empty);
}
