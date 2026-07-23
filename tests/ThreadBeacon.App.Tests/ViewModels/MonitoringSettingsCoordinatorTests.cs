using ThreadBeacon.App.Settings;
using ThreadBeacon.App.ViewModels;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class MonitoringSettingsCoordinatorTests
{
    [Fact]
    public void RefreshIntervalChange_UpdatesTimerWithoutRefreshingOrChangingPauseState()
    {
        var displaySettings = new DisplaySettingsViewModel();
        var monitoring = new MonitoringState();
        monitoring.ToggleCommand.Execute(null);
        TimeSpan? appliedInterval = null;
        int refreshCount = 0;
        using var coordinator = new MonitoringSettingsCoordinator(
            displaySettings,
            interval => appliedInterval = interval,
            () =>
            {
                refreshCount++;
                return Task.CompletedTask;
            });

        displaySettings.RefreshIntervalSeconds = 5;

        Assert.Equal(TimeSpan.FromSeconds(5), appliedInterval);
        Assert.Equal(0, refreshCount);
        Assert.True(monitoring.IsPaused);
    }

    [Fact]
    public async Task MaximumTaskCountChange_PerformsOneBaselineRefreshOnly()
    {
        var displaySettings = new DisplaySettingsViewModel();
        var refreshed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int refreshCount = 0;
        int intervalChangeCount = 0;
        using var coordinator = new MonitoringSettingsCoordinator(
            displaySettings,
            _ => intervalChangeCount++,
            () =>
            {
                refreshCount++;
                refreshed.TrySetResult();
                return Task.CompletedTask;
            });

        displaySettings.MaximumTaskCount = 12;
        await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, refreshCount);
        Assert.Equal(0, intervalChangeCount);
    }

    [Fact]
    public async Task JustCompletedRetentionChange_PerformsOneBaselineRefreshOnly()
    {
        var displaySettings = new DisplaySettingsViewModel();
        var monitoring = new MonitoringState();
        monitoring.ToggleCommand.Execute(null);
        var refreshed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int refreshCount = 0;
        using var coordinator = new MonitoringSettingsCoordinator(
            displaySettings,
            _ => { },
            () =>
            {
                refreshCount++;
                refreshed.TrySetResult();
                return Task.CompletedTask;
            });

        displaySettings.JustCompletedRetentionMinutes = 3;
        await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, refreshCount);
        Assert.True(monitoring.IsPaused);
    }

    [Fact]
    public void Dispose_StopsApplyingChanges()
    {
        var displaySettings = new DisplaySettingsViewModel();
        int actionCount = 0;
        var coordinator = new MonitoringSettingsCoordinator(
            displaySettings,
            _ => actionCount++,
            () =>
            {
                actionCount++;
                return Task.CompletedTask;
            });
        coordinator.Dispose();

        displaySettings.RefreshIntervalSeconds = 5;
        displaySettings.MaximumTaskCount = 12;

        Assert.Equal(0, actionCount);
    }
}
