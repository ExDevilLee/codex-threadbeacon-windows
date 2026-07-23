using System.ComponentModel;
using System.Runtime.CompilerServices;
using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Localization;
using ThreadBeacon.App.Theme;

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

public sealed class ThemeSettingOption : INotifyPropertyChanged
{
    private string displayName;

    public ThemeSettingOption(AppTheme value, string displayName)
    {
        Value = value;
        this.displayName = displayName;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppTheme Value { get; }

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

public sealed class DisplaySettingsViewModel : INotifyPropertyChanged
{
    private readonly IDisplaySettingsStore? settingsStore;
    private readonly AppLanguageState? languageState;
    private readonly AppThemeState? themeState;
    private readonly IReadOnlyList<DisplaySettingOption> refreshOptions;
    private readonly IReadOnlyList<DisplaySettingOption> taskCountOptions;
    private readonly IReadOnlyList<DisplaySettingOption> justCompletedRetentionOptions;
    private readonly IReadOnlyList<ThemeSettingOption> themeOptions;
    private DisplaySettings settings;

    public DisplaySettingsViewModel(
        IDisplaySettingsStore? settingsStore = null,
        AppLanguageState? languageState = null,
        AppThemeState? themeState = null)
    {
        this.settingsStore = settingsStore;
        this.languageState = languageState;
        this.themeState = themeState;
        settings = settingsStore?.Load() ?? new DisplaySettings();
        refreshOptions = DisplaySettings.SupportedRefreshIntervalSeconds
            .Select(value => new DisplaySettingOption(value, string.Empty))
            .ToArray();
        taskCountOptions = DisplaySettings.SupportedMaximumTaskCounts
            .Select(value => new DisplaySettingOption(value, string.Empty))
            .ToArray();
        justCompletedRetentionOptions = DisplaySettings.SupportedJustCompletedRetentionMinutes
            .Select(value => new DisplaySettingOption(value, string.Empty))
            .ToArray();
        themeOptions =
        [
            new(AppTheme.System, string.Empty),
            new(AppTheme.Light, string.Empty),
            new(AppTheme.Dark, string.Empty),
        ];
        UpdateLocalizedOptionNames();
        if (languageState is not null)
        {
            languageState.Changed += OnLanguageChanged;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<DisplaySettingOption> RefreshIntervalOptions => refreshOptions;

    public IReadOnlyList<DisplaySettingOption> MaximumTaskCountOptions => taskCountOptions;

    public IReadOnlyList<DisplaySettingOption> JustCompletedRetentionOptions =>
        justCompletedRetentionOptions;

    public IReadOnlyList<LanguageSettingOption> LanguageOptions =>
    [
        new(AppLanguage.System, AppLanguageText.LanguageName(AppLanguage.System)),
        new(AppLanguage.SimplifiedChinese, AppLanguageText.LanguageName(AppLanguage.SimplifiedChinese)),
        new(AppLanguage.English, AppLanguageText.LanguageName(AppLanguage.English)),
    ];

    public IReadOnlyList<ThemeSettingOption> ThemeOptions => themeOptions;

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
                settings = new DisplaySettings(
                    settings.RefreshIntervalSeconds,
                    settings.MaximumTaskCount,
                    settings.Version,
                    value,
                    Theme,
                    settings.UseColorBlindSafeStatusIndicators,
                    settings.JustCompletedRetentionMinutes);
            }
            else
            {
                settings = new DisplaySettings(
                    settings.RefreshIntervalSeconds,
                    settings.MaximumTaskCount,
                    settings.Version,
                    value,
                    settings.Theme,
                    settings.UseColorBlindSafeStatusIndicators,
                    settings.JustCompletedRetentionMinutes);
                settingsStore?.Save(settings);
            }

            OnPropertyChanged();
        }
    }

    public AppLanguage EffectiveLanguage => languageState?.EffectiveLanguage ?? AppLanguage.SimplifiedChinese;

    public AppTheme Theme
    {
        get => themeState?.Preference ?? settings.Theme;
        set
        {
            if (Theme == value)
            {
                return;
            }

            if (themeState is not null)
            {
                themeState.SetPreference(value);
                settings = new DisplaySettings(
                    settings.RefreshIntervalSeconds,
                    settings.MaximumTaskCount,
                    settings.Version,
                    Language,
                    value,
                    settings.UseColorBlindSafeStatusIndicators,
                    settings.JustCompletedRetentionMinutes);
            }
            else
            {
                settings = new DisplaySettings(
                    settings.RefreshIntervalSeconds,
                    settings.MaximumTaskCount,
                    settings.Version,
                    settings.Language,
                    value,
                    settings.UseColorBlindSafeStatusIndicators,
                    settings.JustCompletedRetentionMinutes);
                settingsStore?.Save(settings);
            }

            OnPropertyChanged();
        }
    }

    public AppTheme EffectiveTheme => themeState?.EffectiveTheme ?? settings.Theme;

    public int RefreshIntervalSeconds
    {
        get => settings.RefreshIntervalSeconds;
        set
        {
            var updated = new DisplaySettings(
                value,
                settings.MaximumTaskCount,
                settings.Version,
                Language,
                Theme,
                settings.UseColorBlindSafeStatusIndicators,
                settings.JustCompletedRetentionMinutes);
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
            var updated = new DisplaySettings(
                settings.RefreshIntervalSeconds,
                value,
                settings.Version,
                Language,
                Theme,
                settings.UseColorBlindSafeStatusIndicators,
                settings.JustCompletedRetentionMinutes);
            if (updated.MaximumTaskCount == settings.MaximumTaskCount)
            {
                return;
            }

            settings = updated;
            settingsStore?.Save(settings);
            OnPropertyChanged();
        }
    }

    public int JustCompletedRetentionMinutes
    {
        get => settings.JustCompletedRetentionMinutes;
        set
        {
            var updated = new DisplaySettings(
                settings.RefreshIntervalSeconds,
                settings.MaximumTaskCount,
                settings.Version,
                Language,
                Theme,
                settings.UseColorBlindSafeStatusIndicators,
                value);
            if (updated.JustCompletedRetentionMinutes == settings.JustCompletedRetentionMinutes)
            {
                return;
            }

            settings = updated;
            settingsStore?.Save(settings);
            OnPropertyChanged();
        }
    }

    public bool UseColorBlindSafeStatusIndicators
    {
        get => settings.UseColorBlindSafeStatusIndicators;
        set
        {
            if (settings.UseColorBlindSafeStatusIndicators == value)
            {
                return;
            }

            settings = new DisplaySettings(
                settings.RefreshIntervalSeconds,
                settings.MaximumTaskCount,
                settings.Version,
                Language,
                Theme,
                value,
                settings.JustCompletedRetentionMinutes);
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

        foreach (DisplaySettingOption option in justCompletedRetentionOptions)
        {
            option.SetDisplayName(
                AppLanguageText.JustCompletedRetentionMinutes(language, option.Value));
        }

        foreach (ThemeSettingOption option in themeOptions)
        {
            option.SetDisplayName(AppLanguageText.ThemeName(language, option.Value));
        }
    }
}
