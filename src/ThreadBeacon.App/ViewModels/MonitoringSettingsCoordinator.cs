using System.ComponentModel;

namespace ThreadBeacon.App.ViewModels;

public sealed class MonitoringSettingsCoordinator : IDisposable
{
    private readonly DisplaySettingsViewModel displaySettings;
    private readonly Action<TimeSpan> applyRefreshInterval;
    private readonly Func<Task> requestBaselineRefresh;
    private bool isDisposed;

    public MonitoringSettingsCoordinator(
        DisplaySettingsViewModel displaySettings,
        Action<TimeSpan> applyRefreshInterval,
        Func<Task> requestBaselineRefresh)
    {
        this.displaySettings = displaySettings
            ?? throw new ArgumentNullException(nameof(displaySettings));
        this.applyRefreshInterval = applyRefreshInterval
            ?? throw new ArgumentNullException(nameof(applyRefreshInterval));
        this.requestBaselineRefresh = requestBaselineRefresh
            ?? throw new ArgumentNullException(nameof(requestBaselineRefresh));
        displaySettings.PropertyChanged += OnDisplaySettingsPropertyChanged;
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        displaySettings.PropertyChanged -= OnDisplaySettingsPropertyChanged;
    }

    private async void OnDisplaySettingsPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DisplaySettingsViewModel.RefreshIntervalSeconds))
        {
            applyRefreshInterval(displaySettings.RefreshInterval);
            return;
        }

        if (e.PropertyName is nameof(DisplaySettingsViewModel.MaximumTaskCount)
            or nameof(DisplaySettingsViewModel.Language))
        {
            try
            {
                await requestBaselineRefresh();
            }
            catch
            {
                // Refresh failures are surfaced by the main view model.
            }
        }
    }
}
