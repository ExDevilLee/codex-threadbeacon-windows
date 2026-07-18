using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ThreadBeacon.App.Commands;
using ThreadBeacon.App.Settings;

namespace ThreadBeacon.App.ViewModels;

public sealed class WindowPinState : INotifyPropertyChanged
{
    private readonly IAppSettingsStore settingsStore;
    private bool isPinned;

    public WindowPinState(IAppSettingsStore settingsStore)
    {
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        isPinned = settingsStore.Load().IsWindowPinned;
        ToggleCommand = new RelayCommand(Toggle);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsPinned
    {
        get => isPinned;
        private set
        {
            if (isPinned == value)
            {
                return;
            }

            isPinned = value;
            OnPropertyChanged();
        }
    }

    public ICommand ToggleCommand { get; }

    private void Toggle()
    {
        IsPinned = !IsPinned;
        settingsStore.Save(new AppSettings { IsWindowPinned = IsPinned });
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
