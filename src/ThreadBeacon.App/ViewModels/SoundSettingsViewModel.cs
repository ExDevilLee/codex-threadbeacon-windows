using System.ComponentModel;
using System.IO;
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
        new(CompletionSound.FupicatNotification, "Fupicat Notification"),
        new(CompletionSound.BassguitarNotification, "Bassguitar Notification"),
        new(CompletionSound.Beacon, "Beacon"),
        new(CompletionSound.Chime, "Chime"),
        new(CompletionSound.Pulse, "Pulse"),
        new(CompletionSound.Alert, "Alert"),
        new(CompletionSound.Resolve, "Resolve"),
        new(CompletionSound.Knock, "Knock"),
    ];

    private readonly ISoundNotificationSettingsStore settingsStore;
    private readonly ISoundPlaybackService player;
    private readonly Func<string?> chooseSoundFile;
    private SoundNotificationSettings settings;

    public SoundSettingsViewModel(
        ISoundNotificationSettingsStore settingsStore,
        ISoundPlaybackService player,
        Func<string?>? chooseSoundFile = null)
    {
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        this.player = player ?? throw new ArgumentNullException(nameof(player));
        this.chooseSoundFile = chooseSoundFile ?? (() => null);
        settings = settingsStore.Load();
        PreviewCommand = new RelayCommand(Preview);
        WarningPreviewCommand = new RelayCommand(PreviewWarning);
        SelectCompletionSoundCommand = new RelayCommand(SelectCompletionSound);
        ClearCompletionSoundCommand = new RelayCommand(ClearCompletionSound, () => HasCompletionSoundPath);
        SelectWarningSoundCommand = new RelayCommand(SelectWarningSound);
        ClearWarningSoundCommand = new RelayCommand(ClearWarningSound, () => HasWarningSoundPath);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<CompletionSoundOption> AvailableSounds => SoundOptions;

    public IReadOnlyList<string> SeenEventIds => settings.SeenEventIds;

    public ICommand PreviewCommand { get; }

    public ICommand WarningPreviewCommand { get; }

    public ICommand SelectCompletionSoundCommand { get; }

    public ICommand ClearCompletionSoundCommand { get; }

    public ICommand SelectWarningSoundCommand { get; }

    public ICommand ClearWarningSoundCommand { get; }

    public string CompletionSoundPathText =>
        string.IsNullOrWhiteSpace(settings.CompletionSoundPath)
            ? string.Empty
            : Path.GetFileName(settings.CompletionSoundPath);

    public string WarningSoundPathText =>
        string.IsNullOrWhiteSpace(settings.WarningSoundPath)
            ? string.Empty
            : Path.GetFileName(settings.WarningSoundPath);

    public bool HasCompletionSoundPath => !string.IsNullOrWhiteSpace(settings.CompletionSoundPath);

    public bool HasWarningSoundPath => !string.IsNullOrWhiteSpace(settings.WarningSoundPath);

    public string? CompletionSoundPath => settings.CompletionSoundPath;

    public string? WarningSoundPath => settings.WarningSoundPath;

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
            OnPropertyChanged(nameof(IsWarningConfigurationEnabled));
            OnPropertyChanged(nameof(IsCompletionCategoryEnabled));
            OnPropertyChanged(nameof(IsCompletionSoundEnabled));
            OnPropertyChanged(nameof(IsWarningCategoryEnabled));
            OnPropertyChanged(nameof(IsWarningSoundEnabled));
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
            OnPropertyChanged(nameof(IsCompletionSoundEnabled));
        }
    }

    public bool IsCompletionConfigurationEnabled => IsEnabled;

    public bool IsCompletionCategoryEnabled => IsEnabled;

    public bool IsCompletionSoundEnabled => IsEnabled && IsCompletionEnabled;

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

    public bool IsWarningEnabled
    {
        get => settings.IsWarningEnabled;
        set
        {
            if (settings.IsWarningEnabled == value)
            {
                return;
            }

            settings = settings with { IsWarningEnabled = value };
            Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWarningSoundEnabled));
        }
    }

    public bool IsWarningConfigurationEnabled => IsEnabled;

    public bool IsWarningCategoryEnabled => IsEnabled;

    public bool IsWarningSoundEnabled => IsEnabled && IsWarningEnabled;

    public CompletionSound SelectedWarningSound
    {
        get => settings.SelectedWarningSound;
        set
        {
            if (settings.SelectedWarningSound == value)
            {
                return;
            }

            settings = settings with { SelectedWarningSound = value };
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
        if (!IsCompletionSoundEnabled)
        {
            return;
        }

        try
        {
            player.Play(SelectedCompletionSound, CompletionSoundPath);
        }
        catch
        {
            // Preview is optional and must not destabilize settings interaction.
        }
    }

    private void PreviewWarning()
    {
        if (!IsWarningSoundEnabled)
        {
            return;
        }

        try
        {
            player.Play(SelectedWarningSound, WarningSoundPath);
        }
        catch
        {
            // Preview is optional and must not destabilize settings interaction.
        }
    }

    private void Save() => settingsStore.Save(settings);

    private void SelectCompletionSound()
    {
        if (chooseSoundFile() is string path)
        {
            settings = settings with { CompletionSoundPath = path };
            Save();
            NotifyCustomSoundProperties(completion: true);
        }
    }

    private void ClearCompletionSound()
    {
        settings = settings with { CompletionSoundPath = null };
        Save();
        NotifyCustomSoundProperties(completion: true);
    }

    private void SelectWarningSound()
    {
        if (chooseSoundFile() is string path)
        {
            settings = settings with { WarningSoundPath = path };
            Save();
            NotifyCustomSoundProperties(completion: false);
        }
    }

    private void ClearWarningSound()
    {
        settings = settings with { WarningSoundPath = null };
        Save();
        NotifyCustomSoundProperties(completion: false);
    }

    private void NotifyCustomSoundProperties(bool completion)
    {
        if (completion)
        {
            OnPropertyChanged(nameof(CompletionSoundPath));
            OnPropertyChanged(nameof(CompletionSoundPathText));
            OnPropertyChanged(nameof(HasCompletionSoundPath));
            if (ClearCompletionSoundCommand is RelayCommand command) command.NotifyCanExecuteChanged();
        }
        else
        {
            OnPropertyChanged(nameof(WarningSoundPath));
            OnPropertyChanged(nameof(WarningSoundPathText));
            OnPropertyChanged(nameof(HasWarningSoundPath));
            if (ClearWarningSoundCommand is RelayCommand command) command.NotifyCanExecuteChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
