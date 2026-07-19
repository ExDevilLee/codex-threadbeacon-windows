using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;

namespace ThreadBeacon.App.Sounds;

public interface ICompletionNotificationObserver
{
    void Observe(
        IReadOnlyList<ThreadSnapshot> snapshots,
        RefreshNotificationPolicy policy);
}
