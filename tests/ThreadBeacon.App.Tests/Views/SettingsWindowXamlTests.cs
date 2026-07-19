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

        Assert.Equal(["通用", "提示音"], headers);
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
}
