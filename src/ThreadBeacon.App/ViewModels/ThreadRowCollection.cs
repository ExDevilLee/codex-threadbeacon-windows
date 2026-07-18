using System.Collections.ObjectModel;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed class ThreadRowCollection
{
    public ObservableCollection<ThreadRowViewModel> Items { get; } = [];

    public void Reconcile(IReadOnlyList<ThreadSnapshot> snapshots, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        for (int targetIndex = 0; targetIndex < snapshots.Count; targetIndex++)
        {
            ThreadSnapshot snapshot = snapshots[targetIndex];
            int existingIndex = FindIndex(snapshot.Id, targetIndex);
            if (existingIndex < 0)
            {
                Items.Insert(targetIndex, new ThreadRowViewModel(snapshot, now));
                continue;
            }

            ThreadRowViewModel row = Items[existingIndex];
            row.Update(snapshot, now);
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
