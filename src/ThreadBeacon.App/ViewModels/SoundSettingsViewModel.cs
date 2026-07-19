using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ThreadBeacon.App.Commands;
using ThreadBeacon.App.Sounds;

namespace ThreadBeacon.App.ViewModels;

public sealed record CompletionSoundOption(CompletionSound Value, string DisplayName);

public sealed class SoundSettingsViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyList<CompletionSoundOption> SoundOptions =
    [
        new(CompletionSound.Beacon, "Beacon"),
        new(CompletionSound.Chime, "Chime"),
        new(CompletionSound.Pulse, "Pulse"),
    ];

    private readonly ISoundNotificationSettingsStore settingsStore;
    private readonly ISoundPlaybackService player;
    private SoundNotificationSettings settings;

    public SoundSettingsViewModel(
        ISoundNotificationSettingsStore settingsStore,
        ISoundPlaybackService player)
    {
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        this.player = player ?? throw new ArgumentNullException(nameof(player));
        settings = settingsStore.Load();
        PreviewCommand = new RelayCommand(Preview);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<CompletionSoundOption> AvailableSounds => SoundOptions;

    public IReadOnlyList<string> SeenEventIds => settings.SeenEventIds;

    public ICommand PreviewCommand { get; }

    public bool IsEnabled
    {
        get => settings.IsEnabled;
        set
        {
            if (settings.IsEnabled == value)
            {
                return;
            }

            settings = settings with { IsEnabled = value };
            Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCompletionConfigurationEnabled));
        }
    }

    public bool IsCompletionEnabled
    {
        get => settings.IsCompletionEnabled;
        set
        {
            if (settings.IsCompletionEnabled == value)
            {
                return;
            }

            settings = settings with { IsCompletionEnabled = value };
            Save();
            OnPropertyChanged();
        }
    }

    public bool IsCompletionConfigurationEnabled => IsEnabled;

    public CompletionSound SelectedCompletionSound
    {
        get => settings.SelectedCompletionSound;
        set
        {
            if (settings.SelectedCompletionSound == value)
            {
                return;
            }

            settings = settings with { SelectedCompletionSound = value };
            Save();
            OnPropertyChanged();
        }
    }

    public void ReplaceSeenEventIds(IReadOnlyList<string> eventIds)
    {
        ArgumentNullException.ThrowIfNull(eventIds);
        if (settings.SeenEventIds.SequenceEqual(eventIds, StringComparer.Ordinal))
        {
            return;
        }

        settings = settings with { SeenEventIds = eventIds.ToArray() };
        Save();
    }

    private void Preview()
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            player.Play(SelectedCompletionSound);
        }
        catch
        {
            // Preview is optional and must not destabilize settings interaction.
        }
    }

    private void Save() => settingsStore.Save(settings);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
