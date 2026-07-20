namespace ThreadBeacon.App.ViewModels;

public sealed class SettingsWindowViewModel
{
    public SettingsWindowViewModel(
        DisplaySettingsViewModel display,
        SoundSettingsViewModel sound,
        LoginStartupViewModel? startup = null)
    {
        Display = display ?? throw new ArgumentNullException(nameof(display));
        Sound = sound ?? throw new ArgumentNullException(nameof(sound));
        Startup = startup ?? new LoginStartupViewModel(
            new ThreadBeacon.App.Startup.WindowsLoginStartupService());
    }

    public DisplaySettingsViewModel Display { get; }

    public SoundSettingsViewModel Sound { get; }

    public LoginStartupViewModel Startup { get; }
}
