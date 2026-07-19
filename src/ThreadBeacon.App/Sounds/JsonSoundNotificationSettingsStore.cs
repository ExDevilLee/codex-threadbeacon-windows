using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThreadBeacon.App.Sounds;

public sealed class JsonSoundNotificationSettingsStore : ISoundNotificationSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string settingsPath;

    public JsonSoundNotificationSettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        this.settingsPath = settingsPath;
    }

    public static JsonSoundNotificationSettingsStore CreateDefault()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new JsonSoundNotificationSettingsStore(
            Path.Combine(root, "ThreadBeacon", "sound-settings.json"));
    }

    public SoundNotificationSettings Load()
    {
        try
        {
            string json = File.ReadAllText(settingsPath);
            SoundNotificationSettings settings =
                JsonSerializer.Deserialize<SoundNotificationSettings>(json, SerializerOptions)
                ?? new SoundNotificationSettings();
            return settings with { SeenEventIds = settings.SeenEventIds ?? [] };
        }
        catch (Exception exception) when (IsSettingsException(exception))
        {
            return new SoundNotificationSettings();
        }
    }

    public bool Save(SoundNotificationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(settingsPath, json);
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
}
