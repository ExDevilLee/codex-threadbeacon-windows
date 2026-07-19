using ThreadBeacon.App.Settings;

namespace ThreadBeacon.App.Tests.Settings;

public sealed class JsonDisplaySettingsStoreTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"ThreadBeacon.DisplaySettings.Tests-{Guid.NewGuid():N}");

    [Fact]
    public void DisplaySettings_NormalizesUnsupportedValuesIndependently()
    {
        var settings = new DisplaySettings(3, 12);

        Assert.Equal(2, settings.RefreshIntervalSeconds);
        Assert.Equal(12, settings.MaximumTaskCount);
        Assert.Equal([1, 2, 5, 10], DisplaySettings.SupportedRefreshIntervalSeconds);
        Assert.Equal([4, 8, 12, 20], DisplaySettings.SupportedMaximumTaskCounts);
    }

    [Fact]
    public void Load_WhenFileIsMissing_ReturnsDefaults()
    {
        var store = Store();

        DisplaySettings settings = store.Load();

        Assert.Equal(2, settings.RefreshIntervalSeconds);
        Assert.Equal(8, settings.MaximumTaskCount);
        Assert.Equal(1, settings.Version);
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
