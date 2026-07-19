using System.Xml.Linq;

namespace ThreadBeacon.App.Tests.Views;

public sealed class SettingsWindowXamlTests
{
    private static XDocument LoadDocument() => XDocument.Load(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "SettingsWindow.xaml"));

    [Fact]
    public void Window_HasGeneralAndSoundTabs()
    {
        XDocument document = LoadDocument();
        string[] headers = document.Descendants()
            .Where(element => element.Name.LocalName == "TabItem")
            .Select(element => (string?)element.Attribute("Header"))
            .OfType<string>()
            .ToArray();

        Assert.Equal(["{DynamicResource GeneralTab}", "{DynamicResource SoundsTab}"], headers);
    }

    [Fact]
    public void GeneralTab_BindsRefreshIntervalAndMaximumTaskCount()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("Display.RefreshIntervalOptions", markup, StringComparison.Ordinal);
        Assert.Contains("Display.RefreshIntervalSeconds", markup, StringComparison.Ordinal);
        Assert.Contains("Display.MaximumTaskCountOptions", markup, StringComparison.Ordinal);
        Assert.Contains("Display.MaximumTaskCount", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SoundTab_UsesCategoryAndSoundEnablementBindings()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("Sound.IsCompletionCategoryEnabled", markup, StringComparison.Ordinal);
        Assert.Contains("Sound.IsCompletionSoundEnabled", markup, StringComparison.Ordinal);
        Assert.Contains("Sound.IsWarningCategoryEnabled", markup, StringComparison.Ordinal);
        Assert.Contains("Sound.IsWarningSoundEnabled", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ComboBoxes_VerticallyCenterSelectedText()
    {
        XElement[] comboBoxes = LoadDocument()
            .Descendants()
            .Where(element => element.Name.LocalName == "ComboBox")
            .ToArray();

        Assert.Equal(5, comboBoxes.Length);
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

        Assert.Equal(5, rowDefinitions.Elements().Count());
        XElement languageCombo = Assert.Single(
            generalGrid.Descendants(),
            element => element.Name.LocalName == "ComboBox"
                && (string?)element.Attribute("SelectedValue")
                    == "{Binding Display.Language, Mode=TwoWay}");
        Assert.Equal("2", (string?)languageCombo.Attribute("Grid.Row"));

        XElement maximumCountCombo = Assert.Single(
            generalGrid.Descendants(),
            element => element.Name.LocalName == "ComboBox"
                && (string?)element.Attribute("SelectedValue")
                    == "{Binding Display.MaximumTaskCount, Mode=TwoWay}");
        Assert.Equal("4", (string?)maximumCountCombo.Attribute("Grid.Row"));
    }
}
