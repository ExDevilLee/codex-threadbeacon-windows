using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.AutoRecovery;

public sealed class JsonAutoRecoverySettingsStore : IAutoRecoverySettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string settingsPath;

    public JsonAutoRecoverySettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        this.settingsPath = Path.GetFullPath(settingsPath);
    }

    public static JsonAutoRecoverySettingsStore CreateDefault()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new JsonAutoRecoverySettingsStore(
            Path.Combine(root, "ThreadBeacon", "auto-recovery-settings.json"));
    }

    public AutoRecoverySettings Load(AutoRecoveryPromptLanguage language)
    {
        try
        {
            string json = File.ReadAllText(settingsPath);
            SettingsDocument? document = JsonSerializer.Deserialize<SettingsDocument>(
                json,
                SerializerOptions);
            if (document is null)
            {
                return AutoRecoverySettings.CreateDefault(language);
            }

            var settings = new AutoRecoverySettings(
                document.IsEnabled,
                new Dictionary<AutoRecoveryIncidentType, AutoRecoveryRule>());
            foreach ((AutoRecoveryIncidentType type, AutoRecoveryRule rule) in
                document.Rules ?? [])
            {
                if (Enum.IsDefined(type))
                {
                    settings.SetRule(type, rule, language);
                }
            }

            settings.SynchronizeDefaultPrompts(language);
            return settings;
        }
        catch (Exception exception) when (IsSettingsException(exception))
        {
            return AutoRecoverySettings.CreateDefault(language);
        }
    }

    public bool Save(AutoRecoverySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        try
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var document = new SettingsDocument(
                1,
                settings.IsEnabled,
                settings.Rules.ToDictionary(pair => pair.Key, pair => pair.Value));
            string json = JsonSerializer.Serialize(document, SerializerOptions);
            string temporaryPath = $"{settingsPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, settingsPath, overwrite: true);
            }
            finally
            {
                File.Delete(temporaryPath);
            }

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

    private sealed record SettingsDocument(
        int Version,
        bool IsEnabled,
        Dictionary<AutoRecoveryIncidentType, AutoRecoveryRule>? Rules);
}
