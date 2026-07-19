using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Sounds;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Notifications;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    private readonly DispatcherTimer refreshTimer;
    private readonly WavSoundPlaybackService soundPlayer;
    private readonly MonitoringSettingsCoordinator monitoringSettingsCoordinator;
    private readonly SettingsWindowViewModel settingsViewModel;
    private SettingsWindow? settingsWindow;

    public MainWindow()
    {
        InitializeComponent();

        CodexDataPaths paths = CodexDataPaths.Resolve();
        var loader = new ThreadStatusLoader(
            new SQLiteThreadRepository(paths.StateDatabase),
            new SessionIndexTitleRepository(paths.SessionIndex),
            new RolloutTailParser(),
            logEventRepository: new SQLiteLogEventRepository(paths.LogDatabase));
        var windowPin = new WindowPinState(JsonAppSettingsStore.CreateDefault());
        var monitoring = new MonitoringState();
        var displaySettings = new DisplaySettingsViewModel(
            JsonDisplaySettingsStore.CreateDefault());
        soundPlayer = new WavSoundPlaybackService();
        var soundSettings = new SoundSettingsViewModel(
            JsonSoundNotificationSettingsStore.CreateDefault(),
            soundPlayer);
        var completionNotifications = new CompletionNotificationCoordinator(
            soundSettings,
            soundPlayer);
        settingsViewModel = new SettingsWindowViewModel(displaySettings, soundSettings);
        viewModel = new MainWindowViewModel(
            loader,
            windowPin,
            monitoring,
            completionNotifications,
            JsonThreadListPreferenceStore.CreateDefault(),
            displaySettings: displaySettings);
        DataContext = viewModel;
        monitoring.PropertyChanged += OnMonitoringPropertyChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = displaySettings.RefreshInterval,
        };
        monitoringSettingsCoordinator = new MonitoringSettingsCoordinator(
            displaySettings,
            interval => refreshTimer.Interval = interval,
            () => viewModel.RefreshAsync(RefreshNotificationPolicy.Baseline));
        refreshTimer.Tick += OnRefreshTimerTick;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await viewModel.RefreshAsync(RefreshNotificationPolicy.Baseline);
        if (viewModel.Monitoring.ShouldAutoRefresh)
        {
            refreshTimer.Start();
        }
    }

    private async void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        if (viewModel.Monitoring.ShouldAutoRefresh)
        {
            await viewModel.RefreshAsync(RefreshNotificationPolicy.Notify);
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

        await viewModel.RefreshAsync(RefreshNotificationPolicy.Baseline);
        if (viewModel.Monitoring.ShouldAutoRefresh)
        {
            refreshTimer.Start();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
        viewModel.Monitoring.PropertyChanged -= OnMonitoringPropertyChanged;
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        monitoringSettingsCoordinator.Dispose();
        soundPlayer.Dispose();
    }

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        if (settingsWindow is not null)
        {
            if (settingsWindow.WindowState is WindowState.Minimized)
            {
                settingsWindow.WindowState = WindowState.Normal;
            }

            settingsWindow.Activate();
            return;
        }

        settingsWindow = new SettingsWindow
        {
            Owner = this,
            DataContext = settingsViewModel,
        };
        settingsWindow.Closed += OnSettingsWindowClosed;
        settingsWindow.Show();
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        if (settingsWindow is not null)
        {
            settingsWindow.Closed -= OnSettingsWindowClosed;
            settingsWindow = null;
        }
    }

    private void OnIgnoredTasksButtonClick(object sender, RoutedEventArgs e) =>
        IgnoredTasksPopup.IsOpen = !IgnoredTasksPopup.IsOpen;

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.HasIgnoredThreads)
            && !viewModel.HasIgnoredThreads)
        {
            IgnoredTasksPopup.IsOpen = false;
        }
    }
}

