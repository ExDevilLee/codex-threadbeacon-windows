using System.ComponentModel;
using System.Runtime.CompilerServices;
using ThreadBeacon.App.Settings;

namespace ThreadBeacon.App.ViewModels;

public sealed record DisplaySettingOption(int Value, string DisplayName);

public sealed class DisplaySettingsViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyList<DisplaySettingOption> RefreshOptions =
        DisplaySettings.SupportedRefreshIntervalSeconds
            .Select(value => new DisplaySettingOption(value, $"{value} 秒"))
            .ToArray();
    private static readonly IReadOnlyList<DisplaySettingOption> TaskCountOptions =
        DisplaySettings.SupportedMaximumTaskCounts
            .Select(value => new DisplaySettingOption(value, $"{value} 个"))
            .ToArray();

    private readonly IDisplaySettingsStore? settingsStore;
    private DisplaySettings settings;

    public DisplaySettingsViewModel(IDisplaySettingsStore? settingsStore = null)
    {
        this.settingsStore = settingsStore;
        settings = settingsStore?.Load() ?? new DisplaySettings();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<DisplaySettingOption> RefreshIntervalOptions => RefreshOptions;

    public IReadOnlyList<DisplaySettingOption> MaximumTaskCountOptions => TaskCountOptions;

    public int RefreshIntervalSeconds
    {
        get => settings.RefreshIntervalSeconds;
        set
        {
            var updated = new DisplaySettings(value, settings.MaximumTaskCount, settings.Version);
            if (updated.RefreshIntervalSeconds == settings.RefreshIntervalSeconds)
            {
                return;
            }

            settings = updated;
            settingsStore?.Save(settings);
            OnPropertyChanged();
            OnPropertyChanged(nameof(RefreshInterval));
        }
    }

    public TimeSpan RefreshInterval => TimeSpan.FromSeconds(RefreshIntervalSeconds);

    public int MaximumTaskCount
    {
        get => settings.MaximumTaskCount;
        set
        {
            var updated = new DisplaySettings(settings.RefreshIntervalSeconds, value, settings.Version);
            if (updated.MaximumTaskCount == settings.MaximumTaskCount)
            {
                return;
            }

            settings = updated;
            settingsStore?.Save(settings);
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
