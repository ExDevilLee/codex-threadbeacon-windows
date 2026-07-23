using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.IO;
using Microsoft.Win32;
using ThreadBeacon.App.AutoRecovery;
using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Sounds;
using ThreadBeacon.App.Startup;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.App.Windowing;
using ThreadBeacon.Core.Notifications;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    private readonly DispatcherTimer refreshTimer;
    private readonly WavSoundPlaybackService soundPlayer;
    private readonly MonitoringSettingsCoordinator monitoringSettingsCoordinator;
    private readonly SettingsWindowViewModel settingsViewModel;
    private readonly WindowsLoginStartupService loginStartupService;
    private readonly HttpClient updateHttpClient;
    private readonly WindowPlacementCoordinator windowPlacementCoordinator;
    private readonly ICodexThreadOpener threadOpener;
    private SettingsWindow? settingsWindow;
    private AboutWindow? aboutWindow;
    private bool isPlacementTrackingActive;

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
            JsonDisplaySettingsStore.CreateDefault(),
            App.LanguageState,
            App.ThemeState);
        soundPlayer = new WavSoundPlaybackService();
        var soundSettings = new SoundSettingsViewModel(
            JsonSoundNotificationSettingsStore.CreateDefault(),
            soundPlayer,
            ChooseSoundFile);
        var completionNotifications = new CompletionNotificationCoordinator(
            soundSettings,
            soundPlayer);
        var autoRecoverySettingsStore = JsonAutoRecoverySettingsStore.CreateDefault();
        var autoRecoveryHistoryStore = JsonAutoRecoveryHistoryStore.CreateDefault();
        var autoRecoveryCircuitStore = JsonAutoRecoveryCircuitStore.CreateDefault();
        var autoRecoverySettings = new AutoRecoverySettingsViewModel(
            autoRecoverySettingsStore,
            autoRecoveryHistoryStore,
            displaySettings,
            autoRecoveryCircuitStore);
        var codexAutomation = new WindowsCodexComposerAutomation();
        threadOpener = new WindowsCodexThreadOpener(codexAutomation);
        var autoRecoveryCoordinator = new AutoRecoveryCoordinator(
            () => autoRecoverySettings.Settings,
            new WindowsCodexRecoverySender(
                codexAutomation,
                new RolloutRecoveryEvidenceMonitor(),
                new WindowsRecoveryForegroundSessionFactory()),
            autoRecoveryHistoryStore,
            circuitStore: autoRecoveryCircuitStore);
        loginStartupService = new WindowsLoginStartupService();
        string localSupportPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ThreadBeacon");
        var hookSettings = new CompactionHookSettingsViewModel(
            new CompactionHookConfigurationManager(
                Path.Combine(paths.CodexHome, "hooks.json"),
                Path.Combine(paths.CodexHome, "config.toml"),
                localSupportPath),
            Path.Combine(AppContext.BaseDirectory, "ThreadBeacon.HookBridge.exe"),
            App.LanguageState);
        settingsViewModel = new SettingsWindowViewModel(
            displaySettings,
            soundSettings,
            new LoginStartupViewModel(loginStartupService),
            autoRecoverySettings,
            hookSettings);
        windowPlacementCoordinator = new WindowPlacementCoordinator(
            JsonWindowPlacementStore.CreateDefault(),
            new NativeWindowPlacementPlatform());
        updateHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        SemanticVersion.TryParse(
            Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3),
            out SemanticVersion currentVersion);
        var updateCheck = new UpdateCheckViewModel(
            new GitHubReleaseClient(updateHttpClient),
            currentVersion,
            uri => Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }));
        viewModel = new MainWindowViewModel(
            loader,
            windowPin,
            monitoring,
            completionNotifications,
            JsonThreadListPreferenceStore.CreateDefault(),
            displaySettings: displaySettings,
            updateCheck: updateCheck,
            autoRecoveryObserver: autoRecoveryCoordinator);
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
        SourceInitialized += OnSourceInitialized;
        LocationChanged += OnMainWindowBoundsChanged;
        SizeChanged += OnMainWindowBoundsChanged;
        Closing += OnClosing;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        windowPlacementCoordinator.Restore(new WindowInteropHelper(this).Handle);
        isPlacementTrackingActive = true;
    }

    private void OnMainWindowBoundsChanged(object? sender, EventArgs e)
    {
        if (isPlacementTrackingActive && WindowState is WindowState.Normal)
        {
            windowPlacementCoordinator.Capture(new WindowInteropHelper(this).Handle);
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (isPlacementTrackingActive && WindowState is WindowState.Normal)
        {
            windowPlacementCoordinator.Capture(new WindowInteropHelper(this).Handle);
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await viewModel.RefreshAsync(RefreshNotificationPolicy.Baseline);
        if (viewModel.Monitoring.ShouldAutoRefresh)
        {
            refreshTimer.Start();
        }
        _ = viewModel.UpdateCheck?.CheckAsync();
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
        SourceInitialized -= OnSourceInitialized;
        LocationChanged -= OnMainWindowBoundsChanged;
        SizeChanged -= OnMainWindowBoundsChanged;
        Closing -= OnClosing;
        soundPlayer.Dispose();
        loginStartupService.Dispose();
        updateHttpClient.Dispose();
    }

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        settingsViewModel.AutoRecovery?.RefreshHistory();
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

    private static string? ChooseSoundFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = Application.Current.TryFindResource("SelectCustomSound") as string
                ?? "Choose WAV",
        };
        return dialog.ShowDialog() is true ? dialog.FileName : null;
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        if (settingsWindow is not null)
        {
            settingsWindow.Closed -= OnSettingsWindowClosed;
            settingsWindow = null;
        }
    }

    private void OnAboutButtonClick(object sender, RoutedEventArgs e)
    {
        if (aboutWindow is not null)
        {
            aboutWindow.Activate();
            return;
        }

        aboutWindow = new AboutWindow(viewModel.UpdateCheck) { Owner = this };
        aboutWindow.Closed += OnAboutWindowClosed;
        aboutWindow.Show();
    }

    private void OnAboutWindowClosed(object? sender, EventArgs e)
    {
        if (aboutWindow is not null)
        {
            aboutWindow.Closed -= OnAboutWindowClosed;
            aboutWindow = null;
        }
    }

    private void OnIgnoredTasksButtonClick(object sender, RoutedEventArgs e) =>
        IgnoredTasksPopup.IsOpen = !IgnoredTasksPopup.IsOpen;

    private async void OnTaskRowMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2
            || sender is not FrameworkElement { DataContext: ThreadRowViewModel row }
            || row.IsArchived
            || e.OriginalSource is DependencyObject source && HasButtonAncestor(source))
        {
            return;
        }

        e.Handled = true;
        await threadOpener.OpenAsync(row.Id, row.Title);
    }

    private static bool HasButtonAncestor(DependencyObject source)
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (current is ButtonBase)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.HasIgnoredThreads)
            && !viewModel.HasIgnoredThreads)
        {
            IgnoredTasksPopup.IsOpen = false;
        }
    }
}

