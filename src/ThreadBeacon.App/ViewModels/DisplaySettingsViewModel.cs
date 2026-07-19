using System.ComponentModel;
using System.Runtime.CompilerServices;
using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Localization;

namespace ThreadBeacon.App.ViewModels;

public sealed record DisplaySettingOption(int Value, string DisplayName);
public sealed record LanguageSettingOption(AppLanguage Value, string DisplayName);

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
    private readonly AppLanguageState? languageState;
    private DisplaySettings settings;

    public DisplaySettingsViewModel(
        IDisplaySettingsStore? settingsStore = null,
        AppLanguageState? languageState = null)
    {
        this.settingsStore = settingsStore;
        this.languageState = languageState;
        settings = settingsStore?.Load() ?? new DisplaySettings();
        if (languageState is not null)
        {
            languageState.Changed += OnLanguageChanged;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<DisplaySettingOption> RefreshIntervalOptions =>
        RefreshOptions.Select(option => option with
        {
            DisplayName = AppLanguageText.RefreshSeconds(
                languageState?.EffectiveLanguage ?? AppLanguage.SimplifiedChinese,
                option.Value),
        }).ToArray();

    public IReadOnlyList<DisplaySettingOption> MaximumTaskCountOptions =>
        TaskCountOptions.Select(option => option with
        {
            DisplayName = AppLanguageText.TaskCount(
                languageState?.EffectiveLanguage ?? AppLanguage.SimplifiedChinese,
                option.Value),
        }).ToArray();

    public IReadOnlyList<LanguageSettingOption> LanguageOptions =>
    [
        new(AppLanguage.System, AppLanguageText.LanguageName(AppLanguage.System)),
        new(AppLanguage.SimplifiedChinese, AppLanguageText.LanguageName(AppLanguage.SimplifiedChinese)),
        new(AppLanguage.English, AppLanguageText.LanguageName(AppLanguage.English)),
    ];

    public AppLanguage Language
    {
        get => languageState?.Preference ?? settings.Language;
        set
        {
            if (Language == value)
            {
                return;
            }

            if (languageState is not null)
            {
                languageState.SetPreference(value);
            }
            else
            {
                settings = new DisplaySettings(
                    settings.RefreshIntervalSeconds,
                    settings.MaximumTaskCount,
                    settings.Version,
                    value);
                settingsStore?.Save(settings);
            }

            OnPropertyChanged();
        }
    }

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

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Language));
        OnPropertyChanged(nameof(LanguageOptions));
        OnPropertyChanged(nameof(RefreshIntervalOptions));
        OnPropertyChanged(nameof(MaximumTaskCountOptions));
    }
}
