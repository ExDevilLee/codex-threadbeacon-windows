using System.ComponentModel;
using System.Runtime.CompilerServices;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed class SubagentRowViewModel : INotifyPropertyChanged
{
    private string title = string.Empty;

    public SubagentRowViewModel(SubagentSnapshot snapshot, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Id = snapshot.Id;
        Update(snapshot, now);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Title
    {
        get => title;
        private set => SetField(ref title, value);
    }

    public void Update(SubagentSnapshot snapshot, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!StringComparer.Ordinal.Equals(Id, snapshot.Id))
        {
            throw new ArgumentException("A Subagent row cannot change its thread ID.", nameof(snapshot));
        }

        Title = snapshot.Title;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
