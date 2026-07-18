using System.IO;
using System.Text.Json;

namespace ThreadBeacon.App.Settings;

public sealed class JsonAppSettingsStore : IAppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string settingsPath;

    public JsonAppSettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        this.settingsPath = settingsPath;
    }

    public static JsonAppSettingsStore CreateDefault()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new JsonAppSettingsStore(Path.Combine(root, "ThreadBeacon", "settings.json"));
    }

    public AppSettings Load()
    {
        try
        {
            string json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
        }
        catch (Exception exception) when (IsSettingsException(exception))
        {
            return new AppSettings();
        }
    }

    public bool Save(AppSettings settings)
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
