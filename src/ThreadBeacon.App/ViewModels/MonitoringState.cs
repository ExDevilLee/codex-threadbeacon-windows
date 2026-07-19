using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ThreadBeacon.App.Commands;

namespace ThreadBeacon.App.ViewModels;

public sealed class MonitoringState : INotifyPropertyChanged
{
    private bool isPaused;

    public MonitoringState()
    {
        ToggleCommand = new RelayCommand(Toggle);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsPaused
    {
        get => isPaused;
        private set
        {
            if (isPaused == value)
            {
                return;
            }

            isPaused = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShouldAutoRefresh));
        }
    }

    public bool ShouldAutoRefresh => !IsPaused;

    public ICommand ToggleCommand { get; }

    private void Toggle() => IsPaused = !IsPaused;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
