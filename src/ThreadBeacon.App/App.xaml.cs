using System.Windows;
using ThreadBeacon.App.Localization;
using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Theme;

namespace ThreadBeacon.App;

public partial class App : Application
{
    public static AppLanguageState LanguageState { get; private set; } = null!;
    public static AppThemeState ThemeState { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var store = JsonDisplaySettingsStore.CreateDefault();
        DisplaySettings settings = store.Load();
        LanguageState = new AppLanguageState(
            settings.Language,
            persist: value =>
            {
                DisplaySettings current = store.Load();
                store.Save(new DisplaySettings(
                    current.RefreshIntervalSeconds,
                    current.MaximumTaskCount,
                    current.Version,
                    value,
                    current.Theme,
                    current.UseColorBlindSafeStatusIndicators));
            });
        LanguageState.ApplyResources(this);
        LanguageState.Changed += (_, _) => LanguageState.ApplyResources(this);
        ThemeState = new AppThemeState(
            settings.Theme,
            new WindowsSystemThemeDetector(),
            value =>
            {
                DisplaySettings current = store.Load();
                store.Save(new DisplaySettings(
                    current.RefreshIntervalSeconds,
                    current.MaximumTaskCount,
                    current.Version,
                    current.Language,
                    value,
                    current.UseColorBlindSafeStatusIndicators));
            });
        ThemeState.ApplyResources(this);
        ThemeState.Changed += (_, _) => ThemeState.ApplyResources(this);
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ThemeState?.Dispose();
        base.OnExit(e);
    }
}

