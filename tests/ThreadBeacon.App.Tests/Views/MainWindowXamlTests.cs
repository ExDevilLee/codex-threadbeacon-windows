using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace ThreadBeacon.App.Tests.Views;

public sealed class MainWindowXamlTests
{
    private static XDocument LoadDocument() => XDocument.Load(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "MainWindow.xaml"));

    private static string LoadCodeBehind() => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "MainWindow.xaml.cs.txt"));

    [Fact]
    public void TaskAndSubagentRows_BindColorBlindSafeStatusGlyphsInFixedSlots()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Equal(2, Count(markup, "Text=\"{Binding StatusGlyph}\""));
        Assert.Equal(4, Count(markup,
            "DataContext.DisplaySettings.UseColorBlindSafeStatusIndicators"));
        Assert.Contains("Width=\"18\" Height=\"18\"", markup, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"{Binding StatusLabel}\"", markup, StringComparison.Ordinal);
    }

    private static int Count(string value, string token)
    {
        int count = 0;
        int offset = 0;
        while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += token.Length;
        }

        return count;
    }

    [Fact]
    public void Toolbar_PlacesFavoritesFilterBeforeWindowPinWithDynamicPresentation()
    {
        XDocument document = LoadDocument();
        XElement[] buttons = document.Descendants()
            .Where(element => element.Name.LocalName == "Button")
            .ToArray();
        int favoriteIndex = Array.FindIndex(
            buttons,
            button => (string?)button.Attribute("Command") == "{Binding ToggleFavoritesOnlyCommand}");
        int pinIndex = Array.FindIndex(
            buttons,
            button => (string?)button.Attribute("Command") == "{Binding WindowPin.ToggleCommand}");

        Assert.InRange(favoriteIndex, 0, pinIndex - 1);
        XElement favoriteButton = buttons[favoriteIndex];
        string markup = favoriteButton.ToString(SaveOptions.DisableFormatting);
        Assert.Contains("ShowsFavoritesOnly", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource ShowFavoritesOnly}", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource ShowAllTasks}", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Toolbar_UsesCompactSpacingAtMinimumWindowWidth()
    {
        XDocument document = LoadDocument();
        XElement toolbar = document.Descendants()
            .Single(element => element.Name.LocalName == "StackPanel"
                && (string?)element.Attribute("Grid.Column") == "3"
                && (string?)element.Attribute("Orientation") == "Horizontal"
                && element.Elements().Count(child => child.Name.LocalName == "Button") == 7);
        XElement[] buttons = toolbar.Elements()
            .Where(element => element.Name.LocalName == "Button")
            .ToArray();

        Assert.Equal(7, buttons.Length);
        Assert.All(buttons[..^1], button =>
            Assert.Equal("0,0,4,0", (string?)button.Attribute("Margin")));
        Assert.Null(buttons[^1].Attribute("Margin"));
    }

    [Fact]
    public void Toolbar_WindowPinSelectedStateLayersFillBehindCompletePinOutline()
    {
        XDocument document = LoadDocument();
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        XElement pinButton = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "Button"
                && (string?)element.Attribute("Command") == "{Binding WindowPin.ToggleCommand}");
        XElement fillGlyph = Assert.Single(
            pinButton.Descendants(),
            element => element.Name.LocalName == "TextBlock"
                && (string?)element.Attribute(x + "Name") == "WindowPinFillGlyph");
        XElement outlineGlyph = Assert.Single(
            pinButton.Descendants(),
            element => element.Name.LocalName == "TextBlock"
                && (string?)element.Attribute(x + "Name") == "WindowPinOutlineGlyph");

        Assert.Equal("\uE841", (string?)fillGlyph.Attribute("Text"));
        Assert.Equal(
            "{Binding WindowPin.IsPinned, Converter={StaticResource BooleanToVisibilityConverter}}",
            (string?)fillGlyph.Attribute("Visibility"));
        Assert.Equal("\uE718", (string?)outlineGlyph.Attribute("Text"));
        Assert.Equal(
            (string?)fillGlyph.Attribute("FontSize"),
            (string?)outlineGlyph.Attribute("FontSize"));
    }

    [Fact]
    public void TaskRow_MenuAndGlyphsExposeFavoriteBeforePinAndArchiveState()
    {
        XDocument document = LoadDocument();
        XElement contextMenu = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "ContextMenu");
        XElement[] menuItems = contextMenu.Elements()
            .Where(element => element.Name.LocalName == "MenuItem")
            .ToArray();

        Assert.Equal("{Binding FavoriteCommandLabel}", (string?)menuItems[0].Attribute("Header"));
        Assert.Equal("{Binding ToggleFavoriteCommand}", (string?)menuItems[0].Attribute("Command"));
        Assert.Equal("{Binding PinCommandLabel}", (string?)menuItems[1].Attribute("Header"));
        string markup = document.ToString(SaveOptions.DisableFormatting);
        Assert.Contains("{Binding IsFavorite", markup, StringComparison.Ordinal);
        Assert.Contains("{Binding IsArchived", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TaskRow_HandlesLeftDoubleClickForCodexNavigation()
    {
        XDocument document = LoadDocument();
        XElement rowBorder = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "Border"
                && (string?)element.Attribute("MouseLeftButtonDown")
                    == "OnTaskRowMouseLeftButtonDown");

        Assert.Equal("48", (string?)rowBorder.Attribute("Height"));
    }

    [Fact]
    public void TaskRow_DoubleClickNavigationRejectsArchivedRows()
    {
        string source = LoadCodeBehind();

        Assert.Contains("|| row.IsArchived", source, StringComparison.Ordinal);
        Assert.Contains("await threadOpener.OpenAsync(row.Id, row.Title)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyState_BindsFavoritesSpecificIconAndText()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("EmptyStateIcon", markup, StringComparison.Ordinal);
        Assert.Contains("EmptyStateTitle", markup, StringComparison.Ordinal);
        Assert.Contains("EmptyStateSubtitle", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Footer_PlacesStableHealthControlBeforeUpdatedTime()
    {
        XDocument document = LoadDocument();
        XElement footer = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "Grid"
                && element.Elements().Any(child =>
                    (string?)child.Attribute("Text") == "{Binding StatusText}"));
        XElement[] columns = footer.Elements()
            .Single(element => element.Name.LocalName == "Grid.ColumnDefinitions")
            .Elements()
            .ToArray();
        XElement health = Assert.Single(
            footer.Descendants(),
            element => element.Name.LocalName == "DataSourceHealthControl");
        XElement updated = Assert.Single(
            footer.Descendants(),
            element => (string?)element.Attribute("Text") == "{Binding UpdatedText}");

        Assert.Equal(4, columns.Length);
        Assert.Equal("2", (string?)health.Attribute("Grid.Column"));
        Assert.Equal("{Binding DataSourceHealth}", (string?)health.Attribute("Details"));
        Assert.Equal("3", (string?)updated.Attribute("Grid.Column"));
    }

    [Fact]
    public void Toolbar_UsesSettingsWindowEntryInsteadOfSoundPopup()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.Contains("SettingsButton", markup, StringComparison.Ordinal);
        Assert.Contains("OnSettingsButtonClick", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("SoundButton", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("SoundSettingsPopup", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalizedSurface_HasNoHardCodedChineseText()
    {
        string markup = LoadDocument().ToString(SaveOptions.DisableFormatting);

        Assert.DoesNotMatch(new Regex("[\\u4e00-\\u9fff]"), markup);
        Assert.Contains("{DynamicResource IgnoreTask}", markup, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource RestoreAll}", markup, StringComparison.Ordinal);
    }
}
