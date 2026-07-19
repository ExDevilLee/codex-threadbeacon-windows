using System.Xml.Linq;

namespace ThreadBeacon.App.Tests.Theme;

public sealed class ThemeResourceTests
{
    private static readonly string AppRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "ThreadBeacon.App"));

    [Fact]
    public void LightAndDarkDictionaries_ExposeTheSameSharedBrushKeys()
    {
        string[] expected =
        [
            "WindowBackgroundBrush",
            "SurfaceBrush",
            "PrimaryTextBrush",
            "SecondaryTextBrush",
            "ControlBorderBrush",
        ];

        string[] light = ResourceKeys("Resources", "Theme.Light.xaml");
        string[] dark = ResourceKeys("Resources", "Theme.Dark.xaml");

        Assert.Equal(expected, light);
        Assert.Equal(expected, dark);
    }

    [Fact]
    public void UserInterfaceXaml_UsesDynamicResourcesForThemeBrushes()
    {
        string[] files =
        [
            "MainWindow.xaml",
            "SettingsWindow.xaml",
            Path.Combine("Controls", "DataSourceHealthControl.xaml"),
            Path.Combine("Controls", "SubagentInfoControl.xaml"),
            Path.Combine("Controls", "TokenInfoControl.xaml"),
        ];

        foreach (string file in files)
        {
            string markup = File.ReadAllText(Path.Combine(AppRoot, file));
            Assert.DoesNotContain("StaticResource WindowBackgroundBrush", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StaticResource SurfaceBrush", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StaticResource PrimaryTextBrush", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StaticResource SecondaryTextBrush", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StaticResource ControlBorderBrush", markup, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void UserInterfaceRoots_InheritTheThemePrimaryForeground()
    {
        string[] files =
        [
            "MainWindow.xaml",
            "SettingsWindow.xaml",
            Path.Combine("Controls", "DataSourceHealthControl.xaml"),
            Path.Combine("Controls", "SubagentInfoControl.xaml"),
            Path.Combine("Controls", "TokenInfoControl.xaml"),
        ];

        foreach (string file in files)
        {
            XDocument document = XDocument.Load(Path.Combine(AppRoot, file));
            Assert.Equal(
                "{DynamicResource PrimaryTextBrush}",
                (string?)document.Root!.Attribute("Foreground"));
        }
    }

    [Fact]
    public void MainTaskList_InheritsTheThemePrimaryForeground()
    {
        XDocument document = XDocument.Load(Path.Combine(AppRoot, "MainWindow.xaml"));
        XElement listView = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "ListBox");

        Assert.Equal(
            "{DynamicResource PrimaryTextBrush}",
            (string?)listView.Attribute("Foreground"));
    }

    private static string[] ResourceKeys(params string[] path)
    {
        XDocument document = XDocument.Load(Path.Combine([AppRoot, .. path]));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        return document.Root!
            .Elements()
            .Select(element => (string?)element.Attribute(x + "Key"))
            .Where(key => key is not null)
            .Cast<string>()
            .ToArray();
    }
}
