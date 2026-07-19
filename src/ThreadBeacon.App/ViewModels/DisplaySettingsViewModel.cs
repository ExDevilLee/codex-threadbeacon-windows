using System.ComponentModel;
using System.Runtime.CompilerServices;
using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Localization;

namespace ThreadBeacon.App.ViewModels;

public sealed class DisplaySettingOption : INotifyPropertyChanged
{
    private string displayName;

    public DisplaySettingOption(int value, string displayName)
    {
        Value = value;
        this.displayName = displayName;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Value { get; }

    public string DisplayName
    {
        get => displayName;
        private set
        {
            if (displayName == value)
            {
                return;
            }

            displayName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
        }
    }

    internal void SetDisplayName(string value) => DisplayName = value;
}
public sealed record LanguageSettingOption(AppLanguage Value, string DisplayName);

public sealed class DisplaySettingsViewModel : INotifyPropertyChanged
{
    private readonly IDisplaySettingsStore? settingsStore;
    private readonly AppLanguageState? languageState;
    private readonly IReadOnlyList<DisplaySettingOption> refreshOptions;
    private readonly IReadOnlyList<DisplaySettingOption> taskCountOptions;
    private DisplaySettings settings;

    public DisplaySettingsViewModel(
        IDisplaySettingsStore? settingsStore = null,
        AppLanguageState? languageState = null)
    {
        this.settingsStore = settingsStore;
        this.languageState = languageState;
        settings = settingsStore?.Load() ?? new DisplaySettings();
        refreshOptions = DisplaySettings.SupportedRefreshIntervalSeconds
            .Select(value => new DisplaySettingOption(value, string.Empty))
            .ToArray();
        taskCountOptions = DisplaySettings.SupportedMaximumTaskCounts
            .Select(value => new DisplaySettingOption(value, string.Empty))
            .ToArray();
        UpdateLocalizedOptionNames();
        if (languageState is not null)
        {
            languageState.Changed += OnLanguageChanged;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<DisplaySettingOption> RefreshIntervalOptions => refreshOptions;

    public IReadOnlyList<DisplaySettingOption> MaximumTaskCountOptions => taskCountOptions;

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
        UpdateLocalizedOptionNames();
        OnPropertyChanged(nameof(Language));
    }

    private void UpdateLocalizedOptionNames()
    {
        AppLanguage language = languageState?.EffectiveLanguage ?? AppLanguage.SimplifiedChinese;
        foreach (DisplaySettingOption option in refreshOptions)
        {
            option.SetDisplayName(AppLanguageText.RefreshSeconds(language, option.Value));
        }

        foreach (DisplaySettingOption option in taskCountOptions)
        {
            option.SetDisplayName(AppLanguageText.TaskCount(language, option.Value));
        }
    }
}
