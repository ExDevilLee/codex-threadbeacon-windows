using ThreadBeacon.App.Sounds;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;

namespace ThreadBeacon.App.Tests.Sounds;

public sealed class CompletionNotificationCoordinatorTests
{
    [Fact]
    public void Observe_BaselinePersistsHistoryWithoutPlaying()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings());
        var player = new RecordingSoundPlayer();
        var settings = new SoundSettingsViewModel(store, player);
        var coordinator = new CompletionNotificationCoordinator(settings, player);

        coordinator.Observe(
            [Completed("thread-1", AtSeconds(10))],
            RefreshNotificationPolicy.Baseline);

        Assert.Empty(player.Played);
        Assert.Equal(["done:thread-1:10000"], store.Current.SeenEventIds);
    }

    [Fact]
    public void Observe_NotifyPlaysNewCompletionOnlyOnce()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings());
        var player = new RecordingSoundPlayer();
        var settings = new SoundSettingsViewModel(store, player);
        var coordinator = new CompletionNotificationCoordinator(settings, player);
        ThreadSnapshot snapshot = Completed("thread-1", AtSeconds(10));

        coordinator.Observe([snapshot], RefreshNotificationPolicy.Notify);
        coordinator.Observe([snapshot], RefreshNotificationPolicy.Notify);

        Assert.Equal([CompletionSound.Chime], player.Played);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public void Observe_MultipleNewCompletionsPlayOneSoundAndPersistAll()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings
        {
            SelectedCompletionSound = CompletionSound.Chime,
        });
        var player = new RecordingSoundPlayer();
        var settings = new SoundSettingsViewModel(store, player);
        var coordinator = new CompletionNotificationCoordinator(settings, player);

        coordinator.Observe(
            [Completed("a", AtSeconds(1)), Completed("b", AtSeconds(2))],
            RefreshNotificationPolicy.Notify);

        Assert.Equal([CompletionSound.Chime], player.Played);
        Assert.Equal(["done:a:1000", "done:b:2000"], store.Current.SeenEventIds);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Observe_DisabledNotificationStillPersistsHistory(
        bool isEnabled,
        bool isCompletionEnabled)
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings
        {
            IsEnabled = isEnabled,
            IsCompletionEnabled = isCompletionEnabled,
        });
        var player = new RecordingSoundPlayer();
        var settings = new SoundSettingsViewModel(store, player);
        var coordinator = new CompletionNotificationCoordinator(settings, player);

        coordinator.Observe(
            [Completed("thread-1", AtSeconds(10))],
            RefreshNotificationPolicy.Notify);

        Assert.Empty(player.Played);
        Assert.Equal(["done:thread-1:10000"], store.Current.SeenEventIds);
    }

    [Fact]
    public void Observe_PlaybackFailureDoesNotEscape()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings());
        var player = new RecordingSoundPlayer { ThrowOnPlay = true };
        var settings = new SoundSettingsViewModel(store, player);
        var coordinator = new CompletionNotificationCoordinator(settings, player);

        Exception? exception = Record.Exception(() => coordinator.Observe(
            [Completed("thread-1", AtSeconds(10))],
            RefreshNotificationPolicy.Notify));

        Assert.Null(exception);
        Assert.Equal(["done:thread-1:10000"], store.Current.SeenEventIds);
    }

    [Fact]
    public void Observe_NewIncidentPlaysSelectedWarningSoundOnlyOnce()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings
        {
            SelectedWarningSound = CompletionSound.Pulse,
        });
        var player = new RecordingSoundPlayer();
        var settings = new SoundSettingsViewModel(store, player);
        var coordinator = new CompletionNotificationCoordinator(settings, player);
        ThreadSnapshot retrying = Incident(ServiceIncidentPhase.Retrying);
        ThreadSnapshot failed = Incident(ServiceIncidentPhase.Failed);

        coordinator.Observe([retrying], RefreshNotificationPolicy.Notify);
        coordinator.Observe([failed], RefreshNotificationPolicy.Notify);

        Assert.Equal([CompletionSound.Pulse], player.Played);
        Assert.Equal(["warning:thread-1:turn-a"], store.Current.SeenEventIds);
    }

    [Fact]
    public void Observe_DisabledWarningStillPersistsEpisode()
    {
        var store = new MemorySoundSettingsStore(new SoundNotificationSettings
        {
            IsWarningEnabled = false,
        });
        var player = new RecordingSoundPlayer();
        var settings = new SoundSettingsViewModel(store, player);
        var coordinator = new CompletionNotificationCoordinator(settings, player);

        coordinator.Observe([Incident(ServiceIncidentPhase.Retrying)], RefreshNotificationPolicy.Notify);

        Assert.Empty(player.Played);
        Assert.Equal(["warning:thread-1:turn-a"], store.Current.SeenEventIds);
    }

    private static DateTimeOffset AtSeconds(long seconds) =>
        DateTimeOffset.FromUnixTimeSeconds(seconds);

    private static ThreadSnapshot Completed(string id, DateTimeOffset completedAt) =>
        new(
            id,
            id,
            ThreadStatus.Idle,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            completedAt,
            null,
            completedAt,
            null,
            0,
            RolloutSourceStatus.Healthy);

    private static ThreadSnapshot Incident(ServiceIncidentPhase phase)
    {
        DateTimeOffset occurredAt = AtSeconds(20);
        return new ThreadSnapshot(
            "thread-1",
            "thread-1",
            phase is ServiceIncidentPhase.Failed ? ThreadStatus.Error : ThreadStatus.Warning,
            occurredAt,
            occurredAt,
            occurredAt,
            null,
            null,
            null,
            0,
            RolloutSourceStatus.Healthy,
            serviceIncident: new ServiceIncident(
                "turn-a",
                phase,
                503,
                5,
                5,
                occurredAt));
    }
}

internal sealed class MemorySoundSettingsStore(SoundNotificationSettings initial)
    : ISoundNotificationSettingsStore
{
    public SoundNotificationSettings Current { get; private set; } = initial;

    public int SaveCount { get; private set; }

    public SoundNotificationSettings Load() => Current;

    public bool Save(SoundNotificationSettings settings)
    {
        Current = settings;
        SaveCount++;
        return true;
    }
}

internal sealed class RecordingSoundPlayer : ISoundPlaybackService
{
    public List<CompletionSound> Played { get; } = [];

    public bool ThrowOnPlay { get; init; }

    public bool Play(CompletionSound sound)
    {
        if (ThrowOnPlay)
        {
            throw new InvalidOperationException("Audio device unavailable.");
        }

        Played.Add(sound);
        return true;
    }
}
