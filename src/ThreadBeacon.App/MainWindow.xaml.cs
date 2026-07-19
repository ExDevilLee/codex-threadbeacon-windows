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
        soundPlayer = new WavSoundPlaybackService();
        var soundSettings = new SoundSettingsViewModel(
            JsonSoundNotificationSettingsStore.CreateDefault(),
            soundPlayer);
        var completionNotifications = new CompletionNotificationCoordinator(
            soundSettings,
            soundPlayer);
        viewModel = new MainWindowViewModel(
            loader,
            windowPin,
            monitoring,
            completionNotifications,
            JsonThreadListPreferenceStore.CreateDefault());
        DataContext = viewModel;
        SoundSettingsPanel.DataContext = soundSettings;
        monitoring.PropertyChanged += OnMonitoringPropertyChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

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
        soundPlayer.Dispose();
    }

    private void OnSoundButtonClick(object sender, RoutedEventArgs e) =>
        SoundSettingsPopup.IsOpen = !SoundSettingsPopup.IsOpen;

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

