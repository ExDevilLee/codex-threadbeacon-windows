using System.Collections.ObjectModel;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed class ThreadRowCollection
{
    private readonly Func<string, Task> toggleSubagents;
    private readonly Action<string> togglePin;
    private readonly Action<string> ignore;

    public ThreadRowCollection(
        Func<string, Task>? toggleSubagents = null,
        Action<string>? togglePin = null,
        Action<string>? ignore = null)
    {
        this.toggleSubagents = toggleSubagents ?? (_ => Task.CompletedTask);
        this.togglePin = togglePin ?? (_ => { });
        this.ignore = ignore ?? (_ => { });
    }

    public ObservableCollection<ThreadRowViewModel> Items { get; } = [];

    public void Reconcile(
        IReadOnlyList<ThreadSnapshot> snapshots,
        DateTimeOffset now,
        IReadOnlySet<string>? expandedThreadIds = null,
        IReadOnlySet<string>? pinnedThreadIds = null)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        for (int targetIndex = 0; targetIndex < snapshots.Count; targetIndex++)
        {
            ThreadSnapshot snapshot = snapshots[targetIndex];
            int existingIndex = FindIndex(snapshot.Id, targetIndex);
            if (existingIndex < 0)
            {
                var newRow = new ThreadRowViewModel(
                    snapshot,
                    now,
                    toggleSubagents,
                    togglePin,
                    ignore);
                newRow.SetSubagentExpanded(
                    expandedThreadIds?.Contains(snapshot.Id) is true,
                    isLoading: false);
                newRow.SetPinned(pinnedThreadIds?.Contains(snapshot.Id) is true);
                Items.Insert(targetIndex, newRow);
                continue;
            }

            ThreadRowViewModel row = Items[existingIndex];
            row.Update(snapshot, now);
            row.SetSubagentExpanded(
                expandedThreadIds?.Contains(snapshot.Id) is true,
                isLoading: false);
            row.SetPinned(pinnedThreadIds?.Contains(snapshot.Id) is true);
            if (existingIndex != targetIndex)
            {
                Items.Move(existingIndex, targetIndex);
            }
        }

        while (Items.Count > snapshots.Count)
        {
            Items.RemoveAt(Items.Count - 1);
        }
    }

    private int FindIndex(string id, int startIndex)
    {
        for (int index = startIndex; index < Items.Count; index++)
        {
            if (StringComparer.Ordinal.Equals(Items[index].Id, id))
            {
                return index;
            }
        }

        return -1;
    }
}
