using System.Text.Json;
using System.Text.Json.Nodes;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class CompactionHookConfigurationManagerTests
{
    [Fact]
    public void Install_CreatesConfigurationStableHelperAndBackup()
    {
        using var fixture = new HookConfigurationFixture("""
            { "description": "existing", "hooks": { "Stop": [{ "hooks": [{ "type": "command", "command": "existing.exe" }] }] } }
            """);

        CompactionHookConfigurationStatus status = fixture.Manager.Install(fixture.HelperSourcePath);
        JsonObject root = fixture.ReadHooks();

        Assert.Equal(CompactionHookConfigurationStatus.Configured, status);
        Assert.Equal(CompactionHookConfigurationStatus.Configured, fixture.Manager.GetStatus());
        Assert.Equal("existing", root["description"]?.GetValue<string>());
        Assert.Equal(1, fixture.HandlerCount(root, "Stop"));
        Assert.Equal(1, fixture.HandlerCount(root, "PreCompact"));
        Assert.Equal(1, fixture.HandlerCount(root, "PostCompact"));
        Assert.True(File.Exists(fixture.HelperDestinationPath));
        Assert.True(File.Exists(fixture.BackupPath));
    }

    [Fact]
    public void Install_IsIdempotentAndPreservesExistingHandlers()
    {
        using var fixture = new HookConfigurationFixture("""
            { "hooks": { "PreCompact": [{ "matcher": "manual", "hooks": [{ "type": "command", "command": "existing.exe" }] }] } }
            """);

        fixture.Manager.Install(fixture.HelperSourcePath);
        fixture.Manager.Install(fixture.HelperSourcePath);
        JsonObject root = fixture.ReadHooks();

        Assert.Equal(2, fixture.HandlerCount(root, "PreCompact"));
        Assert.Equal(1, fixture.ManagedHandlerCount(root, "PreCompact"));
        Assert.Equal(1, fixture.ManagedHandlerCount(root, "PostCompact"));
    }

    [Fact]
    public void Uninstall_RemovesOnlyManagedHandlersAndHelper()
    {
        using var fixture = new HookConfigurationFixture("""
            { "hooks": { "PreCompact": [{ "hooks": [{ "type": "command", "command": "existing.exe" }] }] } }
            """);
        fixture.Manager.Install(fixture.HelperSourcePath);

        fixture.Manager.Uninstall();
        JsonObject root = fixture.ReadHooks();

        Assert.Equal(1, fixture.HandlerCount(root, "PreCompact"));
        Assert.Equal(0, fixture.ManagedHandlerCount(root, "PreCompact"));
        Assert.Equal(0, fixture.HandlerCount(root, "PostCompact"));
        Assert.False(File.Exists(fixture.HelperDestinationPath));
        Assert.Equal(CompactionHookConfigurationStatus.NotConfigured, fixture.Manager.GetStatus());
    }

    [Fact]
    public void Install_RejectsMalformedJsonWithoutChangingIt()
    {
        using var fixture = new HookConfigurationFixture("not-json");
        string original = File.ReadAllText(fixture.HooksPath);

        CompactionHookConfigurationException error = Assert.Throws<CompactionHookConfigurationException>(
            () => fixture.Manager.Install(fixture.HelperSourcePath));

        Assert.Equal(CompactionHookConfigurationError.InvalidHooksJson, error.Error);
        Assert.Equal(original, File.ReadAllText(fixture.HooksPath));
    }

    [Fact]
    public void Install_RejectsInlineTomlHooks()
    {
        using var fixture = new HookConfigurationFixture(configToml: "[hooks]\n");

        CompactionHookConfigurationException error = Assert.Throws<CompactionHookConfigurationException>(
            () => fixture.Manager.Install(fixture.HelperSourcePath));

        Assert.Equal(CompactionHookConfigurationError.InlineHooksPresent, error.Error);
        Assert.False(File.Exists(fixture.HooksPath));
    }

    [Fact]
    public void Install_RejectsReparsePointHooksFile()
    {
        using var fixture = new HookConfigurationFixture();
        string target = Path.Combine(fixture.Root, "target.json");
        File.WriteAllText(target, "{}");
        try
        {
            File.CreateSymbolicLink(fixture.HooksPath, target);
        }
        catch (Exception linkError) when (linkError is UnauthorizedAccessException or IOException)
        {
            return;
        }

        CompactionHookConfigurationException error = Assert.Throws<CompactionHookConfigurationException>(
            () => fixture.Manager.Install(fixture.HelperSourcePath));

        Assert.Equal(CompactionHookConfigurationError.UnsafeHooksFile, error.Error);
    }

    [Fact]
    public void Install_DoesNotOverwriteConcurrentExternalChange()
    {
        using var fixture = new HookConfigurationFixture("{ \"hooks\": {} }");
        var manager = fixture.CreateManager(() =>
            File.WriteAllText(fixture.HooksPath, "{ \"description\": \"external\", \"hooks\": {} }"));

        CompactionHookConfigurationException error = Assert.Throws<CompactionHookConfigurationException>(
            () => manager.Install(fixture.HelperSourcePath));

        Assert.Equal(CompactionHookConfigurationError.ConfigurationChanged, error.Error);
        Assert.Equal("external", fixture.ReadHooks()["description"]?.GetValue<string>());
    }

    [Fact]
    public void Install_RejectsMissingHelper()
    {
        using var fixture = new HookConfigurationFixture();

        CompactionHookConfigurationException error = Assert.Throws<CompactionHookConfigurationException>(
            () => fixture.Manager.Install(Path.Combine(fixture.Root, "missing.exe")));

        Assert.Equal(CompactionHookConfigurationError.HelperUnavailable, error.Error);
    }

    private sealed class HookConfigurationFixture : IDisposable
    {
        public HookConfigurationFixture(string? hooksJson = null, string? configToml = null)
        {
            Root = Path.Combine(Path.GetTempPath(), "ThreadBeaconHookConfig", Guid.NewGuid().ToString("N"));
            string codexHome = Path.Combine(Root, ".codex");
            SupportPath = Path.Combine(Root, "ThreadBeacon");
            HooksPath = Path.Combine(codexHome, "hooks.json");
            ConfigPath = Path.Combine(codexHome, "config.toml");
            HelperSourcePath = Path.Combine(Root, "source-helper.exe");
            Directory.CreateDirectory(codexHome);
            File.WriteAllText(HelperSourcePath, "helper");
            if (hooksJson is not null)
            {
                File.WriteAllText(HooksPath, hooksJson);
            }
            if (configToml is not null)
            {
                File.WriteAllText(ConfigPath, configToml);
            }

            Manager = CreateManager();
        }

        public string Root { get; }
        public string SupportPath { get; }
        public string HooksPath { get; }
        public string ConfigPath { get; }
        public string HelperSourcePath { get; }
        public string HelperDestinationPath => Path.Combine(SupportPath, "hooks", "v1", "ThreadBeacon.HookBridge.exe");
        public string BackupPath => Path.Combine(SupportPath, "hook-backups", "hooks.json.latest");
        public CompactionHookConfigurationManager Manager { get; }

        public CompactionHookConfigurationManager CreateManager(Action? beforeReplace = null) =>
            new(HooksPath, ConfigPath, SupportPath, beforeReplace);

        public JsonObject ReadHooks() =>
            JsonNode.Parse(File.ReadAllText(HooksPath))?.AsObject()
                ?? throw new JsonException("Expected object.");

        public int HandlerCount(JsonObject root, string eventName) =>
            EventGroups(root, eventName)
                .Sum(group => group["hooks"]?.AsArray().Count ?? 0);

        public int ManagedHandlerCount(JsonObject root, string eventName) =>
            EventGroups(root, eventName)
                .SelectMany(group => group["hooks"]?.AsArray() ?? [])
                .Count(handler => handler?["command"]?.GetValue<string>() == Manager.ManagedCommand);

        private static IEnumerable<JsonObject> EventGroups(JsonObject root, string eventName) =>
            root["hooks"]?[eventName]?.AsArray()
                .Select(node => node?.AsObject())
                .Where(node => node is not null)
                .Cast<JsonObject>()
                ?? [];

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
