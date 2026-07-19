using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using ThreadBeacon.App.Settings;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    private readonly DispatcherTimer refreshTimer;

    public MainWindow()
    {
        InitializeComponent();

        CodexDataPaths paths = CodexDataPaths.Resolve();
        var loader = new ThreadStatusLoader(
            new SQLiteThreadRepository(paths.StateDatabase),
            new SessionIndexTitleRepository(paths.SessionIndex),
            new RolloutTailParser());
        var windowPin = new WindowPinState(JsonAppSettingsStore.CreateDefault());
        var monitoring = new MonitoringState();
        viewModel = new MainWindowViewModel(loader, windowPin, monitoring);
        DataContext = viewModel;
        monitoring.PropertyChanged += OnMonitoringPropertyChanged;

        refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2),
        };
        refreshTimer.Tick += OnRefreshTimerTick;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await viewModel.RefreshAsync();
        if (viewModel.Monitoring.ShouldAutoRefresh)
        {
            refreshTimer.Start();
        }
    }

    private async void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        if (viewModel.Monitoring.ShouldAutoRefresh)
        {
            await viewModel.RefreshAsync();
        }
    }

    private async void OnMonitoringPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(MonitoringState.IsPaused))
        {
            return;
        }

        if (viewModel.Monitoring.IsPaused)
        {
            refreshTimer.Stop();
            return;
        }

        await viewModel.RefreshAsync();
        if (viewModel.Monitoring.ShouldAutoRefresh)
        {
            refreshTimer.Start();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
        viewModel.Monitoring.PropertyChanged -= OnMonitoringPropertyChanged;
    }
}

