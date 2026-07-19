using System.IO;
using System.Text.Json;

namespace ThreadBeacon.App.Settings;

public sealed class JsonDisplaySettingsStore : IDisplaySettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string settingsPath;

    public JsonDisplaySettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        this.settingsPath = settingsPath;
    }

    public static JsonDisplaySettingsStore CreateDefault()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new JsonDisplaySettingsStore(
            Path.Combine(root, "ThreadBeacon", "display-settings.json"));
    }

    public DisplaySettings Load()
    {
        try
        {
            string json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<DisplaySettings>(json, SerializerOptions)
                ?? new DisplaySettings();
        }
        catch (Exception exception) when (IsSettingsException(exception))
        {
            return new DisplaySettings();
        }
    }

    public bool Save(DisplaySettings settings)
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
