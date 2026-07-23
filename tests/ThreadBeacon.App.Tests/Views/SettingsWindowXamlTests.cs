using System.Xml.Linq;

namespace ThreadBeacon.App.Tests.Views;

public sealed class SettingsWindowXamlTests
{
    private static XDocument LoadDocument() => XDocument.Load(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "SettingsWindow.xaml"));

    [Fact]
    public void Window_HasGeneralSoundAndAutoRecoveryTabs()
    {
        XDocument document = LoadDocument();
        string[] headers = document.Descendants()
            .Where(element => element.Name.LocalName == "TabItem")
            .Select(element => (string?)element.Attribute("Header"))
            .OfType<string>()
            .ToArray();

        Assert.Equal(
            [
                "{DynamicResource GeneralTab}",
                "{DynamicResource SoundsTab}",
                "{DynamicResource AutoRecoveryTab}",
            ],
            headers);
    }

    [Fact]
    public void GeneralTab_BindsRefreshIntervalRetentionAndMaximumTaskCount()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("Display.RefreshIntervalOptions", markup, StringComparison.Ordinal);
        Assert.Contains("Display.RefreshIntervalSeconds", markup, StringComparison.Ordinal);
        Assert.Contains("Display.MaximumTaskCountOptions", markup, StringComparison.Ordinal);
        Assert.Contains("Display.MaximumTaskCount", markup, StringComparison.Ordinal);
        Assert.Contains("Display.JustCompletedRetentionOptions", markup, StringComparison.Ordinal);
        Assert.Contains("Display.JustCompletedRetentionMinutes", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource JustCompletedRetention}", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource JustCompletedRetentionDescription}", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Window_UsesThemeAwareInheritedForeground()
    {
        XElement window = LoadDocument().Root!;

        Assert.Equal(
            "{DynamicResource PrimaryTextBrush}",
            (string?)window.Attribute("Foreground"));
    }

    [Fact]
    public void Window_IsTallEnoughForAllGeneralSettings()
    {
        XElement window = LoadDocument().Root!;

        Assert.True(int.Parse((string)window.Attribute("Height")!) >= 420);
        Assert.True(int.Parse((string)window.Attribute("MinHeight")!) >= 400);
    }

    [Fact]
    public void SoundTab_UsesCategoryAndSoundEnablementBindings()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("Sound.IsCompletionCategoryEnabled", markup, StringComparison.Ordinal);
        Assert.Contains("Sound.IsCompletionSoundEnabled", markup, StringComparison.Ordinal);
        Assert.Contains("Sound.IsWarningCategoryEnabled", markup, StringComparison.Ordinal);
        Assert.Contains("Sound.IsWarningSoundEnabled", markup, StringComparison.Ordinal);
        Assert.Contains("Sound.SelectCompletionSoundCommand", markup, StringComparison.Ordinal);
        Assert.Contains("Sound.ClearCompletionSoundCommand", markup, StringComparison.Ordinal);
        Assert.Contains("Sound.SelectWarningSoundCommand", markup, StringComparison.Ordinal);
        Assert.Contains("Sound.ClearWarningSoundCommand", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AutoRecoveryTab_BindsMasterRulesPromptsAndHistory()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("AutoRecovery.IsEnabled", markup, StringComparison.Ordinal);
        Assert.Contains("AutoRecovery.Rules", markup, StringComparison.Ordinal);
        Assert.Contains("AutoRecovery.History", markup, StringComparison.Ordinal);
        Assert.Contains("AutoRecovery.RefreshHistoryCommand", markup, StringComparison.Ordinal);
        Assert.Contains("IsCircuitBreakerEnabled", markup, StringComparison.Ordinal);
        Assert.Contains("MaximumConsecutiveAttempts", markup, StringComparison.Ordinal);
        Assert.Contains("AutoRecovery.OpenCircuits", markup, StringComparison.Ordinal);
        Assert.Contains("ResetCommand", markup, StringComparison.Ordinal);
        Assert.Contains("MaxLength=\"500\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AutoRecoveryTab_KeepsCircuitControlsOnOneLine()
    {
        XDocument document = LoadDocument();
        XElement rules = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "ItemsControl"
                && (string?)element.Attribute("ItemsSource") == "{Binding AutoRecovery.Rules}");
        XElement labelColumn = rules.Descendants()
            .First(element => element.Name.LocalName == "ColumnDefinition");

        Assert.Equal("180", (string?)labelColumn.Attribute("Width"));
        XElement circuitRow = Assert.Single(
            rules.Descendants(),
            element => element.Name.LocalName == "StackPanel"
                && (string?)element.Attribute("Orientation") == "Horizontal"
                && element.Descendants().Any(descendant =>
                    (string?)descendant.Attribute("Content")
                        == "{DynamicResource CircuitBreakerConsecutiveFailures}"));
        Assert.Equal(
            ["CheckBox", "ComboBox", "TextBlock"],
            circuitRow.Elements().Select(element => element.Name.LocalName));
        XElement attempts = circuitRow.Elements()
            .Single(element => element.Name.LocalName == "ComboBox");
        Assert.Equal(
            "{Binding IsCircuitBreakerEnabled}",
            (string?)attempts.Attribute("IsEnabled"));
        XElement suffix = Assert.Single(
            circuitRow.Elements(),
            element => (string?)element.Attribute("Text")
                == "{DynamicResource CircuitBreakerAttemptsSuffix}");
        Assert.Null((string?)suffix.Attribute("TextWrapping"));
        Assert.DoesNotContain(
            rules.Descendants(),
            element => element.Name.LocalName == "DockPanel");
    }

    [Fact]
    public void AutoRecoveryHistory_UsesLiveLocalizedHeaderTemplates()
    {
        XDocument document = LoadDocument();
        XElement dataGrid = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "DataGrid"
                && (string?)element.Attribute("ItemsSource") == "{Binding AutoRecovery.History}");
        string[] resources = dataGrid.Descendants()
            .Where(element => element.Name.LocalName == "TextBlock")
            .Select(element => (string?)element.Attribute("Text"))
            .OfType<string>()
            .ToArray();

        Assert.Equal(
            [
                "{DynamicResource RecoveryTaskId}",
                "{DynamicResource RecoveryIncident}",
                "{DynamicResource RecoveryStatus}",
                "{DynamicResource RecoveryUpdatedAt}",
            ],
            resources);
    }

    [Fact]
    public void ComboBoxes_VerticallyCenterSelectedText()
    {
        XElement[] comboBoxes = LoadDocument()
            .Descendants()
            .Where(element => element.Name.LocalName == "ComboBox")
            .ToArray();

        Assert.Equal(8, comboBoxes.Length);
        Assert.All(comboBoxes, comboBox =>
            Assert.Equal("Center", (string?)comboBox.Attribute("VerticalContentAlignment")));
    }

    [Fact]
    public void GeneralTab_BindsLanguagePreference()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("Display.LanguageOptions", markup, StringComparison.Ordinal);
        Assert.Contains("Display.Language", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource Language}", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneralTab_BindsLaunchAtLoginPreference()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("Startup.IsEnabled", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource LaunchAtLogin}", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource LaunchAtLoginDescription}", markup, StringComparison.Ordinal);
        XElement launchDescription = Assert.Single(
            LoadDocument().Descendants(),
            element => element.Name.LocalName == "TextBlock"
                && (string?)element.Attribute("Text") == "{DynamicResource LaunchAtLoginDescription}");
        Assert.Equal("Wrap", (string?)launchDescription.Attribute("TextWrapping"));
    }

    [Fact]
    public void GeneralTab_BindsColorBlindSafeStatusPreference()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("Display.UseColorBlindSafeStatusIndicators", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource ColorBlindSafeStatusIndicators}", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource ColorBlindSafeStatusIndicatorsDescription}", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneralTab_BindsCompactionHookStatusAndCommands()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("Hook.StatusText", markup, StringComparison.Ordinal);
        Assert.Contains("Hook.ErrorText", markup, StringComparison.Ordinal);
        Assert.Contains("Hook.EnableCommand", markup, StringComparison.Ordinal);
        Assert.Contains("Hook.RefreshCommand", markup, StringComparison.Ordinal);
        Assert.Contains("Hook.DisableCommand", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource CompactionHookDisclosure}", markup, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneralTab_UsesDedicatedRowsForAllSettings()
    {
        XDocument document = LoadDocument();
        XElement generalGrid = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "Grid"
                && element.Elements().Any(child => child.Name.LocalName == "Grid.RowDefinitions")
                && element.Descendants().Any(child =>
                    (string?)child.Attribute("Text") == "{DynamicResource RefreshInterval}"));
        XElement rowDefinitions = Assert.Single(
            generalGrid.Elements(),
            element => element.Name.LocalName == "Grid.RowDefinitions");

        Assert.Equal(13, rowDefinitions.Elements().Count());
        XElement languageCombo = Assert.Single(
            generalGrid.Descendants(),
            element => element.Name.LocalName == "ComboBox"
                && (string?)element.Attribute("SelectedValue")
                    == "{Binding Display.Language, Mode=TwoWay}");
        Assert.Equal("2", (string?)languageCombo.Attribute("Grid.Row"));

        XElement themeCombo = Assert.Single(
            generalGrid.Descendants(),
            element => element.Name.LocalName == "ComboBox"
                && (string?)element.Attribute("SelectedValue")
                    == "{Binding Display.Theme, Mode=TwoWay}");
        Assert.Equal("4", (string?)themeCombo.Attribute("Grid.Row"));

        XElement launchAtLoginCheckBox = Assert.Single(
            generalGrid.Descendants(),
            element => element.Name.LocalName == "CheckBox"
                && (string?)element.Attribute("IsChecked")
                    == "{Binding Startup.IsEnabled, Mode=TwoWay}");
        Assert.Equal("8", (string?)launchAtLoginCheckBox.Attribute("Grid.Row"));

        XElement colorBlindSafeCheckBox = Assert.Single(
            generalGrid.Descendants(),
            element => element.Name.LocalName == "CheckBox"
                && (string?)element.Attribute("IsChecked")
                    == "{Binding Display.UseColorBlindSafeStatusIndicators, Mode=TwoWay}");
        Assert.Equal("6", (string?)colorBlindSafeCheckBox.Attribute("Grid.Row"));

        XElement maximumCountCombo = Assert.Single(
            generalGrid.Descendants(),
            element => element.Name.LocalName == "ComboBox"
                && (string?)element.Attribute("SelectedValue")
                    == "{Binding Display.MaximumTaskCount, Mode=TwoWay}");
        Assert.Equal("12", (string?)maximumCountCombo.Attribute("Grid.Row"));

        XElement retentionCombo = Assert.Single(
            generalGrid.Descendants(),
            element => element.Name.LocalName == "ComboBox"
                && (string?)element.Attribute("SelectedValue")
                    == "{Binding Display.JustCompletedRetentionMinutes, Mode=TwoWay}");
        Assert.Equal("10", (string?)retentionCombo.Attribute("Grid.Row"));
    }
}
