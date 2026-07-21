using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;

namespace ThreadBeacon.App.AutoRecovery;

public interface IAutoRecoveryObserver
{
    Task ObserveAsync(
        IReadOnlyList<ThreadSnapshot> snapshots,
        RefreshNotificationPolicy policy,
        CancellationToken cancellationToken = default);
}
