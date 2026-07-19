using System.ComponentModel;
using ThreadBeacon.App.Settings;
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
