using System.Xml.Linq;

namespace ThreadBeacon.App.Tests.Views;

public sealed class AboutWindowXamlTests
{
    private static XDocument LoadDocument() => XDocument.Load(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "AboutWindow.xaml"));

    [Fact]
    public void Window_ShowsIconVersionAndLocalizedProjectInformation()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("Resources/AppIcon.ico", markup, StringComparison.Ordinal);
        Assert.Contains("{Binding VersionText}", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource AboutDescription}", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource AboutDisclaimer}", markup, StringComparison.Ordinal);
        Assert.Contains("UpdateCheck.CheckCommand", markup, StringComparison.Ordinal);
        Assert.Contains("UpdateCheck.OpenReleaseCommand", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Window_OffersProjectInformationAndSupportLinks()
    {
        string[] tags = LoadDocument().Descendants()
            .Where(element => element.Name.LocalName == "Button")
            .Select(element => (string?)element.Attribute("Tag"))
            .OfType<string>()
            .ToArray();

        Assert.Contains(tags, tag => tag.EndsWith("/codex-threadbeacon-windows", StringComparison.Ordinal));
        Assert.Contains(tags, tag => tag.EndsWith("/releases", StringComparison.Ordinal));
        Assert.Contains(tags, tag => tag.EndsWith("/PRIVACY.md", StringComparison.Ordinal));
        Assert.Contains(tags, tag => tag.EndsWith("/LICENSE", StringComparison.Ordinal));
        Assert.Contains(tags, tag => tag.EndsWith("/SPONSOR.md", StringComparison.Ordinal));

        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);
        Assert.Contains("{DynamicResource AboutSupportProject}", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource AboutCopyright}", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Window_IsTallEnoughToShowSupportContentWithoutClipping()
    {
        XElement window = LoadDocument().Root!;

        Assert.True(int.Parse((string)window.Attribute("Height")!) >= 550);
        Assert.True(int.Parse((string)window.Attribute("MinHeight")!) >= 500);
    }

    [Fact]
    public void MainToolbar_OpensAboutWindowThroughLocalizedButton()
    {
        string mainWindowPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MainWindow.xaml");
        string markup = XDocument.Load(mainWindowPath).ToString(SaveOptions.DisableFormatting);

        Assert.Contains("OnAboutButtonClick", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource About}", markup, StringComparison.Ordinal);
        Assert.Contains("UpdateCheck.IsUpdateAvailable", markup, StringComparison.Ordinal);
    }
}
