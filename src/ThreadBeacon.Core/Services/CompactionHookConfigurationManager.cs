using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ThreadBeacon.Core.Services;

public enum CompactionHookConfigurationStatus
{
    NotConfigured,
    Configured,
    ExternallyModified,
}

public enum CompactionHookConfigurationError
{
    InvalidHooksJson,
    UnsafeHooksFile,
    InlineHooksPresent,
    HelperUnavailable,
    ConfigurationChanged,
    WriteFailed,
}

public sealed class CompactionHookConfigurationException(
    CompactionHookConfigurationError error,
    Exception? innerException = null) : Exception(error.ToString(), innerException)
{
    public CompactionHookConfigurationError Error { get; } = error;
}

public sealed class CompactionHookConfigurationManager
{
    private static readonly string[] ManagedEvents = ["PreCompact", "PostCompact"];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Regex InlineHooksPattern = new(
        @"^\[\[?\s*hooks(?:\.|\s*\])",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private readonly string hooksPath;
    private readonly string configPath;
    private readonly string supportPath;
    private readonly string helperPath;
    private readonly string backupPath;
    private readonly Action beforeReplace;

    public CompactionHookConfigurationManager(
        string hooksPath,
        string configPath,
        string supportPath,
        Action? beforeReplace = null)
    {
        this.hooksPath = Path.GetFullPath(hooksPath);
        this.configPath = Path.GetFullPath(configPath);
        this.supportPath = Path.GetFullPath(supportPath);
        helperPath = Path.Combine(this.supportPath, "hooks", "v1", "ThreadBeacon.HookBridge.exe");
        backupPath = Path.Combine(this.supportPath, "hook-backups", "hooks.json.latest");
        this.beforeReplace = beforeReplace ?? (() => { });
    }

    public string ManagedCommand => QuoteCommand(helperPath);

    public CompactionHookConfigurationStatus Install(string helperSourcePath)
    {
        RejectInlineHooks();
        byte[]? original = ReadHooksData();
        JsonObject root = DecodeRoot(original);
        InstallHelper(helperSourcePath);
        if (original is not null)
        {
            WriteAtomic(backupPath, original);
        }

        JsonObject hooks = GetOrCreateHooks(root);
        foreach (string eventName in ManagedEvents)
        {
            hooks[eventName] = AddManagedHandler(hooks[eventName]);
        }

        byte[] replacement = JsonSerializer.SerializeToUtf8Bytes(root, JsonOptions);
        beforeReplace();
        byte[]? current = ReadHooksData();
        if (!EqualBytes(original, current))
        {
            throw new CompactionHookConfigurationException(
                CompactionHookConfigurationError.ConfigurationChanged);
        }

        WriteAtomic(hooksPath, replacement);
        return GetStatus();
    }

    public void Uninstall()
    {
        byte[]? original = ReadHooksData();
        if (original is null)
        {
            DeleteOwnedFiles();
            return;
        }

        JsonObject root = DecodeRoot(original);
        if (root["hooks"] is JsonObject hooks)
        {
            foreach (string eventName in ManagedEvents)
            {
                JsonArray? remaining = RemoveManagedHandlers(hooks[eventName]);
                if (remaining is null || remaining.Count == 0)
                {
                    hooks.Remove(eventName);
                }
                else
                {
                    hooks[eventName] = remaining;
                }
            }
        }

        byte[] replacement = JsonSerializer.SerializeToUtf8Bytes(root, JsonOptions);
        beforeReplace();
        byte[]? current = ReadHooksData();
        if (!EqualBytes(original, current))
        {
            throw new CompactionHookConfigurationException(
                CompactionHookConfigurationError.ConfigurationChanged);
        }

        WriteAtomic(hooksPath, replacement);
        DeleteOwnedFiles();
    }

    public CompactionHookConfigurationStatus GetStatus()
    {
        bool helperExists = IsRegularFile(helperPath);
        try
        {
            byte[]? data = ReadHooksData();
            if (data is null)
            {
                return helperExists
                    ? CompactionHookConfigurationStatus.ExternallyModified
                    : CompactionHookConfigurationStatus.NotConfigured;
            }

            JsonObject root = DecodeRoot(data);
            if (root["hooks"] is not JsonObject hooks)
            {
                return helperExists
                    ? CompactionHookConfigurationStatus.ExternallyModified
                    : CompactionHookConfigurationStatus.NotConfigured;
            }

            int[] counts = ManagedEvents
                .Select(eventName => CountManagedHandlers(hooks[eventName]))
                .ToArray();
            if (counts.All(count => count == 0) && !helperExists)
            {
                return CompactionHookConfigurationStatus.NotConfigured;
            }

            return counts.All(count => count == 1) && helperExists
                ? CompactionHookConfigurationStatus.Configured
                : CompactionHookConfigurationStatus.ExternallyModified;
        }
        catch (CompactionHookConfigurationException)
        {
            return helperExists
                ? CompactionHookConfigurationStatus.ExternallyModified
                : CompactionHookConfigurationStatus.NotConfigured;
        }
    }

    private void RejectInlineHooks()
    {
        if (!File.Exists(configPath))
        {
            return;
        }

        try
        {
            foreach (string rawLine in File.ReadLines(configPath))
            {
                string line = rawLine.Trim();
                if (!line.StartsWith('#') && InlineHooksPattern.IsMatch(line))
                {
                    throw new CompactionHookConfigurationException(
                        CompactionHookConfigurationError.InlineHooksPresent);
                }
            }
        }
        catch (CompactionHookConfigurationException)
        {
            throw;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new CompactionHookConfigurationException(
                CompactionHookConfigurationError.UnsafeHooksFile,
                error);
        }
    }

    private byte[]? ReadHooksData()
    {
        if (!File.Exists(hooksPath))
        {
            return null;
        }

        try
        {
            FileAttributes attributes = File.GetAttributes(hooksPath);
            if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            {
                throw new CompactionHookConfigurationException(
                    CompactionHookConfigurationError.UnsafeHooksFile);
            }

            return File.ReadAllBytes(hooksPath);
        }
        catch (CompactionHookConfigurationException)
        {
            throw;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new CompactionHookConfigurationException(
                CompactionHookConfigurationError.UnsafeHooksFile,
                error);
        }
    }

    private static JsonObject DecodeRoot(byte[]? data)
    {
        if (data is null)
        {
            return [];
        }

        try
        {
            return JsonNode.Parse(data)?.AsObject()
                ?? throw new JsonException("The Hook root must be an object.");
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            throw new CompactionHookConfigurationException(
                CompactionHookConfigurationError.InvalidHooksJson,
                error);
        }
    }

    private static JsonObject GetOrCreateHooks(JsonObject root)
    {
        if (root["hooks"] is null)
        {
            var hooks = new JsonObject();
            root["hooks"] = hooks;
            return hooks;
        }

        if (root["hooks"] is JsonObject existing)
        {
            return existing;
        }

        throw new CompactionHookConfigurationException(
            CompactionHookConfigurationError.InvalidHooksJson);
    }

    private JsonArray AddManagedHandler(JsonNode? value)
    {
        JsonArray groups = RemoveManagedHandlers(value) ?? [];
        groups.Add(new JsonObject
        {
            ["matcher"] = "manual|auto",
            ["hooks"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "command",
                    ["command"] = ManagedCommand,
                    ["timeout"] = 3,
                },
            },
        });
        return groups;
    }

    private JsonArray? RemoveManagedHandlers(JsonNode? value)
    {
        if (value is null)
        {
            return [];
        }
        if (value is not JsonArray sourceGroups)
        {
            throw new CompactionHookConfigurationException(
                CompactionHookConfigurationError.InvalidHooksJson);
        }

        var result = new JsonArray();
        foreach (JsonNode? sourceGroup in sourceGroups)
        {
            if (sourceGroup is not JsonObject group)
            {
                throw new CompactionHookConfigurationException(
                    CompactionHookConfigurationError.InvalidHooksJson);
            }

            JsonObject copy = group.DeepClone().AsObject();
            if (copy["hooks"] is not JsonArray handlers)
            {
                result.Add(copy);
                continue;
            }

            for (int index = handlers.Count - 1; index >= 0; index--)
            {
                if (IsManagedHandler(handlers[index]))
                {
                    handlers.RemoveAt(index);
                }
            }
            if (handlers.Count > 0)
            {
                result.Add(copy);
            }
        }
        return result;
    }

    private int CountManagedHandlers(JsonNode? value)
    {
        if (value is not JsonArray groups)
        {
            return 0;
        }

        return groups
            .OfType<JsonObject>()
            .Select(group => group["hooks"] as JsonArray)
            .Where(handlers => handlers is not null)
            .Sum(handlers => handlers!.Count(IsManagedHandler));
    }

    private bool IsManagedHandler(JsonNode? node) =>
        node is JsonObject handler
        && handler["type"]?.GetValue<string>() == "command"
        && handler["command"]?.GetValue<string>() == ManagedCommand;

    private void InstallHelper(string sourcePath)
    {
        string fullSourcePath = Path.GetFullPath(sourcePath);
        if (!IsRegularFile(fullSourcePath))
        {
            throw new CompactionHookConfigurationException(
                CompactionHookConfigurationError.HelperUnavailable);
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(fullSourcePath, helperPath))
        {
            return;
        }

        try
        {
            WriteAtomic(helperPath, File.ReadAllBytes(fullSourcePath));
        }
        catch (CompactionHookConfigurationException)
        {
            throw;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new CompactionHookConfigurationException(
                CompactionHookConfigurationError.WriteFailed,
                error);
        }
    }

    private static bool IsRegularFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }
            FileAttributes attributes = File.GetAttributes(path);
            return (attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void WriteAtomic(string path, byte[] data)
    {
        string? directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new CompactionHookConfigurationException(
                CompactionHookConfigurationError.WriteFailed);
        }

        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(temporaryPath, data);
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new CompactionHookConfigurationException(
                CompactionHookConfigurationError.WriteFailed,
                error);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private void DeleteOwnedFiles()
    {
        TryDelete(helperPath);
        string activePath = Path.Combine(supportPath, "compaction", "v1", "active");
        try
        {
            if (Directory.Exists(activePath))
            {
                Directory.Delete(activePath, recursive: true);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool EqualBytes(byte[]? left, byte[]? right) =>
        left is null ? right is null : right is not null && left.AsSpan().SequenceEqual(right);

    private static string QuoteCommand(string path) => $"\"{path}\"";
}
