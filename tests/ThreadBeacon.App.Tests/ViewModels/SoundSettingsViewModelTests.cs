using ThreadBeacon.App.Sounds;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.App.Tests.Sounds;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class SoundSettingsViewModelTests
{
    [Fact]
    public void Constructor_RestoresSettingsAndExposesThreeSounds()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings
        {
            IsEnabled = false,
            IsCompletionEnabled = false,
            SelectedCompletionSound = CompletionSound.Pulse,
        });

        var viewModel = new SoundSettingsViewModel(store, new RecordingSoundPlayer());

        Assert.False(viewModel.IsEnabled);
        Assert.False(viewModel.IsCompletionEnabled);
        Assert.Equal(CompletionSound.Pulse, viewModel.SelectedCompletionSound);
        Assert.Equal(
            [CompletionSound.Beacon, CompletionSound.Chime, CompletionSound.Pulse],
            viewModel.AvailableSounds.Select(option => option.Value));
    }

    [Fact]
    public void Setters_SavePreferencesImmediately()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings());
        var viewModel = new SoundSettingsViewModel(store, new RecordingSoundPlayer());

        viewModel.IsEnabled = false;
        viewModel.IsCompletionEnabled = false;
        viewModel.SelectedCompletionSound = CompletionSound.Chime;

        Assert.Equal(3, store.SaveCount);
        Assert.False(store.Current.IsEnabled);
        Assert.False(store.Current.IsCompletionEnabled);
        Assert.Equal(CompletionSound.Chime, store.Current.SelectedCompletionSound);
    }

    [Fact]
    public void PreviewCommand_PlaysSelectedSoundWhenEnabled()
    {
        var player = new RecordingSoundPlayer();
        var viewModel = new SoundSettingsViewModel(
            new MemorySoundSettingsStore(new SoundNotificationSettings
            {
                SelectedCompletionSound = CompletionSound.Pulse,
            }),
            player);

        viewModel.PreviewCommand.Execute(null);

        Assert.Equal([CompletionSound.Pulse], player.Played);
    }

    [Fact]
    public void PreviewCommand_DoesNothingWhenGloballyDisabled()
    {
        var player = new RecordingSoundPlayer();
        var viewModel = new SoundSettingsViewModel(
            new MemorySoundSettingsStore(new SoundNotificationSettings
            {
                IsEnabled = false,
            }),
            player);

        viewModel.PreviewCommand.Execute(null);

        Assert.Empty(player.Played);
    }

    [Fact]
    public void PreviewCommand_PlaybackFailureDoesNotEscape()
    {
        var viewModel = new SoundSettingsViewModel(
            new MemorySoundSettingsStore(new SoundNotificationSettings()),
            new RecordingSoundPlayer { ThrowOnPlay = true });

        Exception? exception = Record.Exception(() => viewModel.PreviewCommand.Execute(null));

        Assert.Null(exception);
    }
}
