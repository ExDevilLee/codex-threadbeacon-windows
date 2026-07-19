using ThreadBeacon.App.Sounds;

namespace ThreadBeacon.App.Tests.Sounds;

public sealed class JsonSoundNotificationSettingsStoreTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"threadbeacon-sound-settings-{Guid.NewGuid():N}");

    [Fact]
    public void Load_MissingFileReturnsEnabledDefaults()
    {
        var store = new JsonSoundNotificationSettingsStore(SettingsPath);

        SoundNotificationSettings result = store.Load();

        Assert.True(result.IsEnabled);
        Assert.True(result.IsCompletionEnabled);
        Assert.Equal(CompletionSound.Beacon, result.SelectedCompletionSound);
        Assert.Empty(result.SeenEventIds);
    }

    [Fact]
    public void Load_MalformedFileReturnsEnabledDefaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "not-json");
        var store = new JsonSoundNotificationSettingsStore(SettingsPath);

        SoundNotificationSettings result = store.Load();

        Assert.True(result.IsEnabled);
        Assert.True(result.IsCompletionEnabled);
        Assert.Equal(CompletionSound.Beacon, result.SelectedCompletionSound);
        Assert.Empty(result.SeenEventIds);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsPreferencesAndHistory()
    {
        var store = new JsonSoundNotificationSettingsStore(SettingsPath);
        var expected = new SoundNotificationSettings
        {
            IsEnabled = false,
            IsCompletionEnabled = false,
            SelectedCompletionSound = CompletionSound.Pulse,
            SeenEventIds = ["done:a:1000", "done:b:2000"],
        };

        bool saved = store.Save(expected);
        SoundNotificationSettings result = store.Load();

        Assert.True(saved);
        Assert.Equal(expected.IsEnabled, result.IsEnabled);
        Assert.Equal(expected.IsCompletionEnabled, result.IsCompletionEnabled);
        Assert.Equal(expected.SelectedCompletionSound, result.SelectedCompletionSound);
        Assert.Equal(expected.SeenEventIds, result.SeenEventIds);
        Assert.Contains("\"Pulse\"", File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void Save_CreatesMissingParentDirectory()
    {
        var store = new JsonSoundNotificationSettingsStore(SettingsPath);

        bool result = store.Save(new SoundNotificationSettings());

        Assert.True(result);
        Assert.True(File.Exists(SettingsPath));
    }

    [Fact]
    public void Save_DirectoryAsTargetReturnsFalse()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var store = new JsonSoundNotificationSettingsStore(tempRoot);

        bool result = store.Save(new SoundNotificationSettings());

        Assert.False(result);
    }

    [Fact]
    public void Load_NullHistoryNormalizesToEmptyCollection()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(
            SettingsPath,
            "{\"isEnabled\":true,\"isCompletionEnabled\":true,\"selectedCompletionSound\":\"Chime\",\"seenEventIds\":null}");
        var store = new JsonSoundNotificationSettingsStore(SettingsPath);

        SoundNotificationSettings result = store.Load();

        Assert.Equal(CompletionSound.Chime, result.SelectedCompletionSound);
        Assert.Empty(result.SeenEventIds);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private string SettingsPath => Path.Combine(tempRoot, "nested", "sound-settings.json");
}
