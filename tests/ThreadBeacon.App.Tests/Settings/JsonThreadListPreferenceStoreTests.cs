using ThreadBeacon.App.Settings;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.Settings;

public sealed class JsonThreadListPreferenceStoreTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"threadbeacon-task-preferences-{Guid.NewGuid():N}");

    [Fact]
    public void Load_MissingOrMalformedFileReturnsEmptyPreferences()
    {
        var store = new JsonThreadListPreferenceStore(SettingsPath);
        ThreadListPreferences missing = store.Load();
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "not-json");

        ThreadListPreferences malformed = store.Load();

        Assert.Empty(missing.PinnedThreadIds);
        Assert.Empty(missing.IgnoredRules);
        Assert.Empty(malformed.PinnedThreadIds);
        Assert.Empty(malformed.IgnoredRules);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsRulesWithoutTaskTitles()
    {
        var store = new JsonThreadListPreferenceStore(SettingsPath);
        var expected = new ThreadListPreferences(
            pinnedThreadIds: ["pinned-id"],
            ignoredRules: new Dictionary<string, IgnoredThreadRule>(StringComparer.Ordinal)
            {
                ["ignored-id"] = new(
                    "ignored-id",
                    DateTimeOffset.FromUnixTimeSeconds(1234),
                    ThreadIgnoreMode.UntilNextTurn),
            });

        bool saved = store.Save(expected);
        ThreadListPreferences loaded = store.Load();
        string json = File.ReadAllText(SettingsPath);

        Assert.True(saved);
        Assert.Equal(["pinned-id"], loaded.PinnedThreadIds);
        Assert.Equal(expected.IgnoredRules["ignored-id"], loaded.IgnoredRules["ignored-id"]);
        Assert.DoesNotContain("taskTitle", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UntilNextTurn", json);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private string SettingsPath => Path.Combine(tempRoot, "nested", "thread-list-preferences.v1.json");
}
