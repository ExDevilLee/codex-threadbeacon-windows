using ThreadBeacon.App.Settings;
using ThreadBeacon.App.Localization;
using ThreadBeacon.App.Theme;

namespace ThreadBeacon.App.Tests.Settings;

public sealed class JsonDisplaySettingsStoreTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"ThreadBeacon.DisplaySettings.Tests-{Guid.NewGuid():N}");

    [Fact]
    public void DisplaySettings_NormalizesUnsupportedValuesIndependently()
    {
        var settings = new DisplaySettings(3, 12, justCompletedRetentionMinutes: 7);

        Assert.Equal(2, settings.RefreshIntervalSeconds);
        Assert.Equal(12, settings.MaximumTaskCount);
        Assert.Equal(1, settings.JustCompletedRetentionMinutes);
        Assert.Equal([1, 2, 5, 10], DisplaySettings.SupportedRefreshIntervalSeconds);
        Assert.Equal([4, 8, 12, 20], DisplaySettings.SupportedMaximumTaskCounts);
        Assert.Equal([1, 2, 3, 4, 5], DisplaySettings.SupportedJustCompletedRetentionMinutes);
    }

    [Fact]
    public void Load_WhenFileIsMissing_ReturnsDefaults()
    {
        var store = Store();

        DisplaySettings settings = store.Load();

        Assert.Equal(2, settings.RefreshIntervalSeconds);
        Assert.Equal(8, settings.MaximumTaskCount);
        Assert.Equal(1, settings.JustCompletedRetentionMinutes);
        Assert.Equal(1, settings.Version);
        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.False(settings.UseColorBlindSafeStatusIndicators);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsJustCompletedRetention()
    {
        var store = Store();

        Assert.True(store.Save(new DisplaySettings(justCompletedRetentionMinutes: 4)));

        Assert.Equal(4, store.Load().JustCompletedRetentionMinutes);
        Assert.Contains("\"justCompletedRetentionMinutes\": 4", File.ReadAllText(
            Path.Combine(tempDirectory, "display-settings.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsSupportedValues()
    {
        var store = Store();

        bool saved = store.Save(new DisplaySettings(5, 20));
        DisplaySettings settings = store.Load();

        Assert.True(saved);
        Assert.Equal(5, settings.RefreshIntervalSeconds);
        Assert.Equal(20, settings.MaximumTaskCount);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsLanguagePreference()
    {
        var store = Store();

        Assert.True(store.Save(new DisplaySettings(5, 20, language: AppLanguage.English)));

        Assert.Equal(AppLanguage.English, store.Load().Language);
        Assert.Contains("\"language\": \"en\"", File.ReadAllText(
            Path.Combine(tempDirectory, "display-settings.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsThemeWithoutChangingOtherPreferences()
    {
        var store = Store();

        Assert.True(store.Save(new DisplaySettings(
            5,
            20,
            language: AppLanguage.English,
            theme: AppTheme.Dark)));
        DisplaySettings settings = store.Load();

        Assert.Equal(5, settings.RefreshIntervalSeconds);
        Assert.Equal(20, settings.MaximumTaskCount);
        Assert.Equal(AppLanguage.English, settings.Language);
        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.Contains("\"theme\": \"dark\"", File.ReadAllText(
            Path.Combine(tempDirectory, "display-settings.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsColorBlindSafeStatusPreference()
    {
        var store = Store();

        Assert.True(store.Save(new DisplaySettings(
            5,
            20,
            language: AppLanguage.English,
            theme: AppTheme.Dark,
            useColorBlindSafeStatusIndicators: true)));
        DisplaySettings settings = store.Load();

        Assert.True(settings.UseColorBlindSafeStatusIndicators);
        Assert.Equal(AppLanguage.English, settings.Language);
        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.Contains("\"useColorBlindSafeStatusIndicators\": true", File.ReadAllText(
            Path.Combine(tempDirectory, "display-settings.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void Load_UnknownThemeFallsBackToSystem()
    {
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(
            Path.Combine(tempDirectory, "display-settings.json"),
            """
            { "version": 1, "refreshIntervalSeconds": 5, "maximumTaskCount": 12, "theme": "unsupported" }
            """);

        DisplaySettings settings = Store().Load();

        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.Equal(5, settings.RefreshIntervalSeconds);
        Assert.Equal(12, settings.MaximumTaskCount);
    }

    [Fact]
    public void Load_NormalizesUnsupportedPersistedValues()
    {
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(
            Path.Combine(tempDirectory, "display-settings.json"),
            """
            { "version": 1, "refreshIntervalSeconds": 7, "maximumTaskCount": 12 }
            """);

        DisplaySettings settings = Store().Load();

        Assert.Equal(2, settings.RefreshIntervalSeconds);
        Assert.Equal(12, settings.MaximumTaskCount);
        Assert.Equal(1, settings.JustCompletedRetentionMinutes);
    }

    [Fact]
    public void Load_WhenJsonIsInvalid_ReturnsDefaults()
    {
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(Path.Combine(tempDirectory, "display-settings.json"), "not-json");

        DisplaySettings settings = Store().Load();

        Assert.Equal(2, settings.RefreshIntervalSeconds);
        Assert.Equal(8, settings.MaximumTaskCount);
    }

    [Fact]
    public void Save_WhenParentPathIsAFile_ReturnsFalse()
    {
        Directory.CreateDirectory(tempDirectory);
        string blockedParent = Path.Combine(tempDirectory, "blocked");
        File.WriteAllText(blockedParent, "blocked");
        var store = new JsonDisplaySettingsStore(
            Path.Combine(blockedParent, "display-settings.json"));

        bool saved = store.Save(new DisplaySettings(10, 4));

        Assert.False(saved);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private JsonDisplaySettingsStore Store() => new(
        Path.Combine(tempDirectory, "display-settings.json"));
}
