using ThreadBeacon.App.Localization;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class CompactionHookSettingsViewModelTests
{
    [Fact]
    public void EnableAndDisableCommandsPublishLiveStatus()
    {
        using var fixture = new HookSettingsFixture();
        var language = new AppLanguageState(AppLanguage.English);
        var viewModel = new CompactionHookSettingsViewModel(
            fixture.Manager,
            fixture.HelperSource,
            language);

        Assert.Equal(CompactionHookConfigurationStatus.NotConfigured, viewModel.Status);
        Assert.Equal("Not configured", viewModel.StatusText);

        viewModel.EnableCommand.Execute(null);

        Assert.Equal(CompactionHookConfigurationStatus.Configured, viewModel.Status);
        Assert.Equal("Enabled", viewModel.StatusText);
        Assert.False(viewModel.HasError);

        viewModel.DisableCommand.Execute(null);

        Assert.Equal(CompactionHookConfigurationStatus.NotConfigured, viewModel.Status);
        Assert.Equal("Not configured", viewModel.StatusText);
    }

    [Fact]
    public void FailedInstallPublishesStableLocalizedError()
    {
        using var fixture = new HookSettingsFixture(hooksText: "not-json");
        var language = new AppLanguageState(AppLanguage.English);
        var viewModel = new CompactionHookSettingsViewModel(
            fixture.Manager,
            fixture.HelperSource,
            language);

        viewModel.EnableCommand.Execute(null);

        Assert.True(viewModel.HasError);
        Assert.Contains("valid JSON", viewModel.ErrorText, StringComparison.Ordinal);

        language.SetPreference(AppLanguage.SimplifiedChinese);

        Assert.Contains("JSON", viewModel.ErrorText, StringComparison.Ordinal);
        Assert.DoesNotContain("valid JSON", viewModel.ErrorText, StringComparison.Ordinal);
    }

    [Fact]
    public void RefreshReportsExternalModification()
    {
        using var fixture = new HookSettingsFixture();
        var viewModel = new CompactionHookSettingsViewModel(
            fixture.Manager,
            fixture.HelperSource,
            new AppLanguageState(AppLanguage.English));
        viewModel.EnableCommand.Execute(null);
        File.WriteAllText(fixture.HooksPath, "{ \"hooks\": {} }");

        viewModel.RefreshCommand.Execute(null);

        Assert.Equal(CompactionHookConfigurationStatus.ExternallyModified, viewModel.Status);
        Assert.Equal("Needs attention", viewModel.StatusText);
    }

    private sealed class HookSettingsFixture : IDisposable
    {
        public HookSettingsFixture(string? hooksText = null)
        {
            Root = Path.Combine(Path.GetTempPath(), "ThreadBeaconHookSettings", Guid.NewGuid().ToString("N"));
            string codexHome = Path.Combine(Root, ".codex");
            string support = Path.Combine(Root, "ThreadBeacon");
            HooksPath = Path.Combine(codexHome, "hooks.json");
            Directory.CreateDirectory(codexHome);
            if (hooksText is not null)
            {
                File.WriteAllText(HooksPath, hooksText);
            }
            HelperSource = Path.Combine(Root, "ThreadBeacon.HookBridge.exe");
            File.WriteAllText(HelperSource, "helper");
            Manager = new CompactionHookConfigurationManager(
                HooksPath,
                Path.Combine(codexHome, "config.toml"),
                support);
        }

        public string Root { get; }
        public string HooksPath { get; }
        public string HelperSource { get; }
        public CompactionHookConfigurationManager Manager { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
