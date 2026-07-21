using System.Text.Json;
using ThreadBeacon.App.AutoRecovery;
using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.Tests.AutoRecovery;

public sealed class JsonAutoRecoverySettingsStoreTests
{
    [Fact]
    public void Load_MissingFileReturnsDisabledLocalizedDefaults()
    {
        string path = TempPath();
        var store = new JsonAutoRecoverySettingsStore(path);

        AutoRecoverySettings settings = store.Load(AutoRecoveryPromptLanguage.English);

        Assert.False(settings.IsEnabled);
        Assert.StartsWith("The previous request", settings.RuleFor(AutoRecoveryIncidentType.Http400).Prompt);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsRulesAndCustomPrompt()
    {
        string path = TempPath();
        try
        {
            var store = new JsonAutoRecoverySettingsStore(path);
            AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
                AutoRecoveryPromptLanguage.English);
            settings.IsEnabled = true;
            settings.SetRule(
                AutoRecoveryIncidentType.Http503,
                new AutoRecoveryRule(true, "Continue this task.", AutoRecoveryPromptSource.Custom));

            Assert.True(store.Save(settings));
            AutoRecoverySettings loaded = store.Load(AutoRecoveryPromptLanguage.SimplifiedChinese);

            Assert.True(loaded.IsEnabled);
            Assert.Equal("Continue this task.", loaded.RuleFor(AutoRecoveryIncidentType.Http503).Prompt);
            Assert.Equal(AutoRecoveryPromptSource.Custom, loaded.RuleFor(AutoRecoveryIncidentType.Http503).PromptSource);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_InvalidDocumentFallsBackWithoutThrowing()
    {
        string path = TempPath();
        try
        {
            File.WriteAllText(path, "{not-json");
            var store = new JsonAutoRecoverySettingsStore(path);

            AutoRecoverySettings settings = store.Load(AutoRecoveryPromptLanguage.English);

            Assert.False(settings.IsEnabled);
            Assert.Equal(5, settings.Rules.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_DocumentWithoutRulesRestoresDefaults()
    {
        string path = TempPath();
        try
        {
            File.WriteAllText(path, "{\"version\":1,\"isEnabled\":true}");
            var store = new JsonAutoRecoverySettingsStore(path);

            AutoRecoverySettings settings = store.Load(AutoRecoveryPromptLanguage.English);

            Assert.True(settings.IsEnabled);
            Assert.Equal(5, settings.Rules.Count);
            Assert.StartsWith("The previous request", settings.RuleFor(AutoRecoveryIncidentType.Http400).Prompt);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SavedDocumentDoesNotContainMachineOrUserPaths()
    {
        string path = TempPath();
        try
        {
            var store = new JsonAutoRecoverySettingsStore(path);
            store.Save(AutoRecoverySettings.CreateDefault(AutoRecoveryPromptLanguage.English));

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            Assert.True(document.RootElement.TryGetProperty("rules", out _));
            Assert.DoesNotContain(Environment.UserName, File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TempPath() => Path.Combine(
        Path.GetTempPath(),
        $"threadbeacon-auto-recovery-{Guid.NewGuid():N}.json");
}
