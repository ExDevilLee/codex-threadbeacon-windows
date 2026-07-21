namespace ThreadBeacon.App.ViewModels;

public sealed class SettingsWindowViewModel
{
    public SettingsWindowViewModel(
        DisplaySettingsViewModel display,
        SoundSettingsViewModel sound,
        LoginStartupViewModel? startup = null,
        AutoRecoverySettingsViewModel? autoRecovery = null)
    {
        Display = display ?? throw new ArgumentNullException(nameof(display));
        Sound = sound ?? throw new ArgumentNullException(nameof(sound));
        Startup = startup ?? new LoginStartupViewModel(
            new ThreadBeacon.App.Startup.WindowsLoginStartupService());
        AutoRecovery = autoRecovery;
    }

    public DisplaySettingsViewModel Display { get; }

    public SoundSettingsViewModel Sound { get; }

    public LoginStartupViewModel Startup { get; }

    public AutoRecoverySettingsViewModel? AutoRecovery { get; }
}
