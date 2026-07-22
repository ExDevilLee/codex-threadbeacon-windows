using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.Core.Tests.AutoRecovery;

public sealed class AutoRecoverySettingsTests
{
    [Fact]
    public void Defaults_AreDisabledAndKeepHttp503OptedOut()
    {
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.English);

        Assert.False(settings.IsEnabled);
        Assert.True(settings.RuleFor(AutoRecoveryIncidentType.Http400).IsEnabled);
        Assert.True(settings.RuleFor(AutoRecoveryIncidentType.Http429).IsEnabled);
        Assert.False(settings.RuleFor(AutoRecoveryIncidentType.Http503).IsEnabled);
        Assert.True(settings.RuleFor(AutoRecoveryIncidentType.OtherHttp).IsEnabled);
        Assert.True(settings.RuleFor(AutoRecoveryIncidentType.ModelCapacity).IsEnabled);
        Assert.True(settings.RuleFor(AutoRecoveryIncidentType.StreamDisconnected).IsEnabled);
        Assert.Equal(
            "The connection was interrupted and all retries failed. Please continue the unfinished task.",
            settings.RuleFor(AutoRecoveryIncidentType.StreamDisconnected).Prompt);
    }

    [Fact]
    public void Constructor_MissingStreamDisconnectRuleUsesLocalizedDefault()
    {
        var legacyRules = Enum.GetValues<AutoRecoveryIncidentType>()
            .Where(type => type is not AutoRecoveryIncidentType.StreamDisconnected)
            .ToDictionary(
                type => type,
                type => AutoRecoverySettings.DefaultRule(type, AutoRecoveryPromptLanguage.English));

        var settings = new AutoRecoverySettings(false, legacyRules);

        Assert.True(settings.RuleFor(AutoRecoveryIncidentType.StreamDisconnected).IsEnabled);
        Assert.False(string.IsNullOrWhiteSpace(
            settings.RuleFor(AutoRecoveryIncidentType.StreamDisconnected).Prompt));
    }

    [Fact]
    public void SynchronizeDefaults_ChangesOnlyPromptsThatWereNotCustomized()
    {
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.SimplifiedChinese);
        settings.SetRule(
            AutoRecoveryIncidentType.Http400,
            new AutoRecoveryRule(true, "Keep this exact text", AutoRecoveryPromptSource.Custom));

        settings.SynchronizeDefaultPrompts(AutoRecoveryPromptLanguage.English);

        Assert.Equal("Keep this exact text", settings.RuleFor(AutoRecoveryIncidentType.Http400).Prompt);
        Assert.StartsWith(
            "The previous request",
            settings.RuleFor(AutoRecoveryIncidentType.Http429).Prompt,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetRule_InvalidPromptRestoresLocalizedDefault(string prompt)
    {
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.English);

        settings.SetRule(
            AutoRecoveryIncidentType.Http400,
            new AutoRecoveryRule(true, prompt, AutoRecoveryPromptSource.Custom),
            AutoRecoveryPromptLanguage.English);

        Assert.Equal(
            AutoRecoveryPromptSource.DefaultValue,
            settings.RuleFor(AutoRecoveryIncidentType.Http400).PromptSource);
    }

    [Fact]
    public void SetRule_TrimsAndLimitsCustomPrompt()
    {
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.English);

        settings.SetRule(
            AutoRecoveryIncidentType.Http400,
            new AutoRecoveryRule(true, "  Continue safely.  ", AutoRecoveryPromptSource.Custom));

        Assert.Equal("Continue safely.", settings.RuleFor(AutoRecoveryIncidentType.Http400).Prompt);
        settings.SetRule(
            AutoRecoveryIncidentType.Http400,
            new AutoRecoveryRule(true, new string('x', 501), AutoRecoveryPromptSource.Custom),
            AutoRecoveryPromptLanguage.English);
        Assert.Equal(
            AutoRecoveryPromptSource.DefaultValue,
            settings.RuleFor(AutoRecoveryIncidentType.Http400).PromptSource);
    }

    [Fact]
    public void Policy_SendsOnlyWhenMasterAndIncidentRuleAreEnabled()
    {
        AutoRecoverySettings settings = AutoRecoverySettings.CreateDefault(
            AutoRecoveryPromptLanguage.English);
        var candidate = new AutoRecoveryCandidate(
            "thread-1",
            "episode-1",
            AutoRecoveryIncidentType.Http400,
            "Title",
            @"C:\Codex\rollout.jsonl",
            DateTimeOffset.UnixEpoch);

        Assert.Equal(AutoRecoveryDecision.Disabled, AutoRecoveryPolicy.Evaluate(candidate, settings));

        settings.IsEnabled = true;
        AutoRecoveryDecision enabled = AutoRecoveryPolicy.Evaluate(candidate, settings);
        Assert.Equal(AutoRecoveryDecisionKind.Send, enabled.Kind);
        Assert.Equal(settings.RuleFor(AutoRecoveryIncidentType.Http400).Prompt, enabled.Prompt);

        settings.SetRule(
            AutoRecoveryIncidentType.Http400,
            settings.RuleFor(AutoRecoveryIncidentType.Http400) with { IsEnabled = false });
        Assert.Equal(AutoRecoveryDecision.Disabled, AutoRecoveryPolicy.Evaluate(candidate, settings));
    }
}
