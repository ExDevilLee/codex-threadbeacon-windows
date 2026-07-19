using ThreadBeacon.App.Commands;

namespace ThreadBeacon.App.ViewModels;

public sealed class IgnoredThreadRowViewModel
{
    public IgnoredThreadRowViewModel(string id, string title, Action<string> restore)
    {
        Id = id;
        Title = string.IsNullOrWhiteSpace(title) ? $"任务 {id[..Math.Min(8, id.Length)]}" : title;
        RestoreCommand = new RelayCommand(() => restore(id));
    }

    public string Id { get; }

    public string Title { get; }

    public RelayCommand RestoreCommand { get; }
}
