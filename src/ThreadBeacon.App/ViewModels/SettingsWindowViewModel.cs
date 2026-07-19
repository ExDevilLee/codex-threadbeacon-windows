namespace ThreadBeacon.App.ViewModels;

public sealed class SettingsWindowViewModel
{
    public SettingsWindowViewModel(
        DisplaySettingsViewModel display,
        SoundSettingsViewModel sound)
    {
        Display = display ?? throw new ArgumentNullException(nameof(display));
        Sound = sound ?? throw new ArgumentNullException(nameof(sound));
    }

    public DisplaySettingsViewModel Display { get; }

    public SoundSettingsViewModel Sound { get; }
}
