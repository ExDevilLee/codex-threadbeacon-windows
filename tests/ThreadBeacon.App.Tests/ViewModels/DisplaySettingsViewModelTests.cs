using System.ComponentModel;
using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Localization;
using ThreadBeacon.App.Theme;
using ThreadBeacon.App.ViewModels;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class DisplaySettingsViewModelTests
{
    [Fact]
    public void Constructor_RestoresSettingsAndExposesSupportedOptions()
    {
        var viewModel = new DisplaySettingsViewModel(
            new MemoryDisplaySettingsStore(new DisplaySettings(5, 12)));

        Assert.Equal(5, viewModel.RefreshIntervalSeconds);
        Assert.Equal(TimeSpan.FromSeconds(5), viewModel.RefreshInterval);
        Assert.Equal(12, viewModel.MaximumTaskCount);
        Assert.Equal([1, 2, 5, 10], viewModel.RefreshIntervalOptions.Select(option => option.Value));
        Assert.Equal(["1 秒", "2 秒", "5 秒", "10 秒"],
            viewModel.RefreshIntervalOptions.Select(option => option.DisplayName));
        Assert.Equal([4, 8, 12, 20], viewModel.MaximumTaskCountOptions.Select(option => option.Value));
        Assert.Equal(["4 个", "8 个", "12 个", "20 个"],
            viewModel.MaximumTaskCountOptions.Select(option => option.DisplayName));
    }

    [Fact]
    public void Setters_SaveImmediatelyAndNotifyOnlyChangedProperties()
    {
        var store = new MemoryDisplaySettingsStore(new DisplaySettings());
        var viewModel = new DisplaySettingsViewModel(store);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.RefreshIntervalSeconds = 5;
        viewModel.MaximumTaskCount = 20;
        viewModel.MaximumTaskCount = 20;

        Assert.Equal(2, store.SaveCount);
        Assert.Equal(5, store.Current.RefreshIntervalSeconds);
        Assert.Equal(20, store.Current.MaximumTaskCount);
        Assert.Contains(nameof(DisplaySettingsViewModel.RefreshIntervalSeconds), changed);
        Assert.Contains(nameof(DisplaySettingsViewModel.RefreshInterval), changed);
        Assert.Contains(nameof(DisplaySettingsViewModel.MaximumTaskCount), changed);
    }

    [Fact]
    public void Language_UpdatesStateAndLocalizedDisplayOptions()
    {
        var state = new AppLanguageState(AppLanguage.System, "en-US");
        var viewModel = new DisplaySettingsViewModel(
            new MemoryDisplaySettingsStore(new DisplaySettings()),
            state);

        viewModel.Language = AppLanguage.English;

        Assert.Equal(AppLanguage.English, state.Preference);
        Assert.Equal("1 sec", viewModel.RefreshIntervalOptions[0].DisplayName);
        Assert.Equal("4 tasks", viewModel.MaximumTaskCountOptions[0].DisplayName);
    }

    [Fact]
    public void LanguageChange_UpdatesExistingDisplayOptionInstances()
    {
        var state = new AppLanguageState(AppLanguage.SimplifiedChinese, "zh-CN");
        var viewModel = new DisplaySettingsViewModel(
            new MemoryDisplaySettingsStore(new DisplaySettings()),
            state);
        DisplaySettingOption refreshOption = viewModel.RefreshIntervalOptions[0];
        DisplaySettingOption taskOption = viewModel.MaximumTaskCountOptions[0];

        viewModel.Language = AppLanguage.English;

        Assert.Same(refreshOption, viewModel.RefreshIntervalOptions[0]);
        Assert.Same(taskOption, viewModel.MaximumTaskCountOptions[0]);
        Assert.Equal("1 sec", refreshOption.DisplayName);
        Assert.Equal("4 tasks", taskOption.DisplayName);
    }

    [Fact]
    public void OtherDisplaySetters_PreserveThemePreference()
    {
        var store = new MemoryDisplaySettingsStore(
            new DisplaySettings(theme: AppTheme.Dark));
        var viewModel = new DisplaySettingsViewModel(store);

        viewModel.RefreshIntervalSeconds = 5;
        viewModel.MaximumTaskCount = 20;

        Assert.Equal(AppTheme.Dark, store.Current.Theme);
    }

    [Fact]
    public void ThemeOptions_UseActiveLanguageAndPersistSelection()
    {
        var language = new AppLanguageState(AppLanguage.English, "en-US");
        var store = new MemoryDisplaySettingsStore(new DisplaySettings(), saveResult: true);
        var detector = new TestThemeDetector(AppTheme.Light);
        var theme = new AppThemeState(
            AppTheme.System,
            detector,
            value => store.Save(new DisplaySettings(theme: value)));
        var viewModel = new DisplaySettingsViewModel(store, language, theme);

        Assert.Equal(
            [AppTheme.System, AppTheme.Light, AppTheme.Dark],
            viewModel.ThemeOptions.Select(option => option.Value));
        Assert.Equal(["Follow system / System", "Light", "Dark"],
            viewModel.ThemeOptions.Select(option => option.DisplayName));

        viewModel.Theme = AppTheme.Dark;

        Assert.Equal(AppTheme.Dark, theme.Preference);
        Assert.Equal(AppTheme.Dark, store.Current.Theme);
    }

    [Fact]
    public void LanguageChange_UpdatesExistingThemeOptionsAndPreservesSelection()
    {
        var language = new AppLanguageState(AppLanguage.SimplifiedChinese, "zh-CN");
        var detector = new TestThemeDetector(AppTheme.Light);
        var theme = new AppThemeState(AppTheme.Dark, detector);
        var viewModel = new DisplaySettingsViewModel(
            new MemoryDisplaySettingsStore(new DisplaySettings(theme: AppTheme.Dark)),
            language,
            theme);
        IReadOnlyList<ThemeSettingOption> options = viewModel.ThemeOptions;
        ThemeSettingOption selectedOption = options.Single(option => option.Value == AppTheme.Dark);

        viewModel.Language = AppLanguage.English;

        Assert.Same(options, viewModel.ThemeOptions);
        Assert.Same(selectedOption, viewModel.ThemeOptions.Single(option => option.Value == AppTheme.Dark));
        Assert.Equal("Dark", selectedOption.DisplayName);
        Assert.Equal(AppTheme.Dark, viewModel.Theme);
    }

    private sealed class TestThemeDetector(AppTheme initial) : IAppThemeDetector
    {
        public event EventHandler? Changed
        {
            add { }
            remove { }
        }

        public AppTheme CurrentTheme { get; private set; } = initial;

        public AppTheme ReadCurrentTheme() => CurrentTheme;

        public void Dispose()
        {
        }
    }

    [Fact]
    public void Setter_WhenSaveFails_KeepsCurrentProcessValue()
    {
        var store = new MemoryDisplaySettingsStore(new DisplaySettings(), saveResult: false);
        var viewModel = new DisplaySettingsViewModel(store);

        viewModel.RefreshIntervalSeconds = 10;

        Assert.Equal(10, viewModel.RefreshIntervalSeconds);
        Assert.Equal(1, store.SaveCount);
    }

    private sealed class MemoryDisplaySettingsStore(
        DisplaySettings initial,
        bool saveResult = true) : IDisplaySettingsStore
    {
        public DisplaySettings Current { get; private set; } = initial;

        public int SaveCount { get; private set; }

        public DisplaySettings Load() => Current;

        public bool Save(DisplaySettings settings)
        {
            SaveCount++;
            Current = settings;
            return saveResult;
        }
    }
}
