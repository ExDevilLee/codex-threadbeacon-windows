using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Settings;

public sealed class JsonThreadListPreferenceStore : IThreadListPreferenceStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string settingsPath;

    public JsonThreadListPreferenceStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        this.settingsPath = settingsPath;
    }

    public static JsonThreadListPreferenceStore CreateDefault()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new JsonThreadListPreferenceStore(
            Path.Combine(root, "ThreadBeacon", "thread-list-preferences.v1.json"));
    }

    public ThreadListPreferences Load()
    {
        try
        {
            string json = File.ReadAllText(settingsPath);
            PreferenceData data = JsonSerializer.Deserialize<PreferenceData>(json, SerializerOptions) ?? new();
            return new ThreadListPreferences(
                data.PinnedThreadIds,
                data.FavoriteThreadIds,
                data.ShowsFavoritesOnly,
                data.IgnoredRules);
        }
        catch (Exception exception) when (IsSettingsException(exception))
        {
            return new ThreadListPreferences();
        }
    }

    public bool Save(ThreadListPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        try
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var data = new PreferenceData
            {
                PinnedThreadIds = preferences.PinnedThreadIds.Order(StringComparer.Ordinal).ToArray(),
                FavoriteThreadIds = preferences.FavoriteThreadIds.Order(StringComparer.Ordinal).ToArray(),
                ShowsFavoritesOnly = preferences.ShowsFavoritesOnly,
                IgnoredRules = new Dictionary<string, IgnoredThreadRule>(
                    preferences.IgnoredRules,
                    StringComparer.Ordinal),
            };
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(data, SerializerOptions));
            return true;
        }
        catch (Exception exception) when (IsSettingsException(exception))
        {
            return false;
        }
    }

    private static bool IsSettingsException(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException
            or ArgumentException;

    private sealed class PreferenceData
    {
        public IReadOnlyList<string> PinnedThreadIds { get; set; } = [];

        public IReadOnlyList<string> FavoriteThreadIds { get; set; } = [];

        public bool ShowsFavoritesOnly { get; set; }

        public Dictionary<string, IgnoredThreadRule> IgnoredRules { get; set; } =
            new(StringComparer.Ordinal);
    }
}
