using System.IO;
using System.Text.Json;

namespace ThreadBeacon.App.Windowing;

public sealed class JsonWindowPlacementStore : IWindowPlacementStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string settingsPath;

    public JsonWindowPlacementStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        this.settingsPath = settingsPath;
    }

    public static JsonWindowPlacementStore CreateDefault()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new JsonWindowPlacementStore(
            Path.Combine(root, "ThreadBeacon", "window-placement.json"));
    }

    public WindowPlacement? Load()
    {
        try
        {
            string json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<WindowPlacement>(json, SerializerOptions);
        }
        catch (Exception exception) when (IsSettingsException(exception))
        {
            return null;
        }
    }

    public bool Save(WindowPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        try
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(placement, SerializerOptions);
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
