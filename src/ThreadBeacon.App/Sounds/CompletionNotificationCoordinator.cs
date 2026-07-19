using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;

namespace ThreadBeacon.App.Sounds;

public sealed class CompletionNotificationCoordinator : ICompletionNotificationObserver
{
    private readonly SoundSettingsViewModel settings;
    private readonly ISoundPlaybackService player;
    private readonly CompletionNotificationTracker tracker;

    public CompletionNotificationCoordinator(
        SoundSettingsViewModel settings,
        ISoundPlaybackService player)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.player = player ?? throw new ArgumentNullException(nameof(player));
        tracker = new CompletionNotificationTracker(settings.SeenEventIds);
    }

    public void Observe(
        IReadOnlyList<ThreadSnapshot> snapshots,
        RefreshNotificationPolicy policy)
    {
        string[] previousHistory = tracker.SeenEventIds.ToArray();
        CompletionNotificationEvent? notification = tracker.Observe(snapshots, policy);

        if (!previousHistory.SequenceEqual(tracker.SeenEventIds, StringComparer.Ordinal))
        {
            settings.ReplaceSeenEventIds(tracker.SeenEventIds);
        }

        if (notification is null || !settings.IsEnabled || !settings.IsCompletionEnabled)
        {
            return;
        }

        try
        {
            player.Play(settings.SelectedCompletionSound);
        }
        catch
        {
            // Audio failures must not interrupt the refresh path.
        }
    }
}
