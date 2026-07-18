using ThreadBeacon.App.Settings;

namespace ThreadBeacon.App.Tests.Settings;

public sealed class JsonAppSettingsStoreTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"ThreadBeacon.App.Tests-{Guid.NewGuid():N}");

    [Fact]
    public void Load_WhenFileIsMissing_ReturnsDefaults()
    {
        var store = new JsonAppSettingsStore(Path.Combine(tempDirectory, "settings.json"));

        AppSettings settings = store.Load();

        Assert.False(settings.IsWindowPinned);
        Assert.Equal(1, settings.Version);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsPinnedState()
    {
        var store = new JsonAppSettingsStore(Path.Combine(tempDirectory, "settings.json"));

        bool saved = store.Save(new AppSettings { IsWindowPinned = true });
        AppSettings settings = store.Load();

        Assert.True(saved);
        Assert.True(settings.IsWindowPinned);
        Assert.Equal(1, settings.Version);
    }

    [Fact]
    public void Load_WhenJsonIsInvalid_ReturnsDefaults()
    {
        string settingsPath = Path.Combine(tempDirectory, "settings.json");
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(settingsPath, "not-json");

        AppSettings settings = new JsonAppSettingsStore(settingsPath).Load();

        Assert.False(settings.IsWindowPinned);
        Assert.Equal(1, settings.Version);
    }

    [Fact]
    public void Save_WhenParentPathIsAFile_ReturnsFalse()
    {
        Directory.CreateDirectory(tempDirectory);
        string blockedParent = Path.Combine(tempDirectory, "blocked");
        File.WriteAllText(blockedParent, "blocked");
        var store = new JsonAppSettingsStore(Path.Combine(blockedParent, "settings.json"));

        bool saved = store.Save(new AppSettings { IsWindowPinned = true });

        Assert.False(saved);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
