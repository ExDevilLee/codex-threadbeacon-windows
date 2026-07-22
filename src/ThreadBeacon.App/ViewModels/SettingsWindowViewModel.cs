namespace ThreadBeacon.App.ViewModels;

public sealed class SettingsWindowViewModel
{
    public SettingsWindowViewModel(
        DisplaySettingsViewModel display,
        SoundSettingsViewModel sound,
        LoginStartupViewModel? startup = null,
        AutoRecoverySettingsViewModel? autoRecovery = null,
        CompactionHookSettingsViewModel? hook = null)
    {
        Display = display ?? throw new ArgumentNullException(nameof(display));
        Sound = sound ?? throw new ArgumentNullException(nameof(sound));
        Startup = startup ?? new LoginStartupViewModel(
            new ThreadBeacon.App.Startup.WindowsLoginStartupService());
        AutoRecovery = autoRecovery;
        Hook = hook;
    }

    public DisplaySettingsViewModel Display { get; }

    public SoundSettingsViewModel Sound { get; }

    public LoginStartupViewModel Startup { get; }

    public AutoRecoverySettingsViewModel? AutoRecovery { get; }

    public CompactionHookSettingsViewModel? Hook { get; }
}
