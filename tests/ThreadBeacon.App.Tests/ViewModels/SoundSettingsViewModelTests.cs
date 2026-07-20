using ThreadBeacon.App.Sounds;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.App.Tests.Sounds;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class SoundSettingsViewModelTests
{
    [Fact]
    public void Constructor_RestoresSettingsAndExposesEightSounds()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings
        {
            IsEnabled = false,
            IsCompletionEnabled = false,
            SelectedCompletionSound = CompletionSound.Pulse,
            IsWarningEnabled = false,
            SelectedWarningSound = CompletionSound.Chime,
        });

        var viewModel = new SoundSettingsViewModel(store, new RecordingSoundPlayer());

        Assert.False(viewModel.IsEnabled);
        Assert.False(viewModel.IsCompletionEnabled);
        Assert.Equal(CompletionSound.Pulse, viewModel.SelectedCompletionSound);
        Assert.False(viewModel.IsWarningEnabled);
        Assert.Equal(CompletionSound.Chime, viewModel.SelectedWarningSound);
        Assert.Equal(
            [
                CompletionSound.FupicatNotification,
                CompletionSound.BassguitarNotification,
                CompletionSound.Beacon,
                CompletionSound.Chime,
                CompletionSound.Pulse,
                CompletionSound.Alert,
                CompletionSound.Resolve,
                CompletionSound.Knock,
            ],
            viewModel.AvailableSounds.Select(option => option.Value));
    }

    [Fact]
    public void Setters_SavePreferencesImmediately()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings());
        var viewModel = new SoundSettingsViewModel(store, new RecordingSoundPlayer());

        viewModel.IsEnabled = false;
        viewModel.IsCompletionEnabled = false;
        viewModel.SelectedCompletionSound = CompletionSound.Beacon;
        viewModel.IsWarningEnabled = false;
        viewModel.SelectedWarningSound = CompletionSound.Resolve;

        Assert.Equal(5, store.SaveCount);
        Assert.False(store.Current.IsEnabled);
        Assert.False(store.Current.IsCompletionEnabled);
        Assert.Equal(CompletionSound.Beacon, store.Current.SelectedCompletionSound);
        Assert.False(store.Current.IsWarningEnabled);
        Assert.Equal(CompletionSound.Resolve, store.Current.SelectedWarningSound);
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
    public void CategoryAndSoundEnablement_RequireExpectedSwitches()
    {
        var viewModel = new SoundSettingsViewModel(
            new MemorySoundSettingsStore(new SoundNotificationSettings
            {
                IsEnabled = true,
                IsCompletionEnabled = false,
                IsWarningEnabled = true,
            }),
            new RecordingSoundPlayer());

        Assert.True(viewModel.IsCompletionCategoryEnabled);
        Assert.False(viewModel.IsCompletionSoundEnabled);
        Assert.True(viewModel.IsWarningCategoryEnabled);
        Assert.True(viewModel.IsWarningSoundEnabled);

        viewModel.IsEnabled = false;

        Assert.False(viewModel.IsCompletionCategoryEnabled);
        Assert.False(viewModel.IsCompletionSoundEnabled);
        Assert.False(viewModel.IsWarningCategoryEnabled);
        Assert.False(viewModel.IsWarningSoundEnabled);
    }

    [Fact]
    public void PreviewCommands_DoNothingWhenTheirCategoryIsDisabled()
    {
        var player = new RecordingSoundPlayer();
        var viewModel = new SoundSettingsViewModel(
            new MemorySoundSettingsStore(new SoundNotificationSettings
            {
                IsEnabled = true,
                IsCompletionEnabled = false,
                IsWarningEnabled = false,
            }),
            player);

        viewModel.PreviewCommand.Execute(null);
        viewModel.WarningPreviewCommand.Execute(null);

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

    [Fact]
    public void WarningPreviewCommand_PlaysSelectedWarningSoundWhenEnabled()
    {
        var player = new RecordingSoundPlayer();
        var viewModel = new SoundSettingsViewModel(
            new MemorySoundSettingsStore(new SoundNotificationSettings
            {
                SelectedWarningSound = CompletionSound.Pulse,
            }),
            player);

        viewModel.WarningPreviewCommand.Execute(null);

        Assert.Equal([CompletionSound.Pulse], player.Played);
    }

    [Fact]
    public void CustomSoundCommands_SaveFilenameAndCanClearIt()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings());
        var viewModel = new SoundSettingsViewModel(
            store,
            new RecordingSoundPlayer(),
            () => @"C:\Sounds\complete.wav");

        viewModel.SelectCompletionSoundCommand.Execute(null);

        Assert.Equal(@"C:\Sounds\complete.wav", store.Current.CompletionSoundPath);
        Assert.Equal("complete.wav", viewModel.CompletionSoundPathText);
        Assert.True(viewModel.HasCompletionSoundPath);

        viewModel.ClearCompletionSoundCommand.Execute(null);

        Assert.Null(store.Current.CompletionSoundPath);
        Assert.False(viewModel.HasCompletionSoundPath);
    }
}
