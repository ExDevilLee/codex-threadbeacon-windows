using System.ComponentModel;

namespace ThreadBeacon.App.ViewModels;

public sealed class LoginStartupViewModel : INotifyPropertyChanged
{
    private readonly ThreadBeacon.App.Startup.WindowsLoginStartupService service;

    public LoginStartupViewModel(ThreadBeacon.App.Startup.WindowsLoginStartupService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsEnabled
    {
        get => service.IsEnabled;
        set
        {
            if (IsEnabled == value)
            {
                return;
            }

            service.SetEnabled(value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }
    }
}
