using System.Xml.Linq;

namespace ThreadBeacon.App.Tests.Views;

public sealed class MainWindowXamlTests
{
    private static XDocument LoadDocument() => XDocument.Load(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "MainWindow.xaml"));

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
        Assert.Contains("仅显示收藏", markup, StringComparison.Ordinal);
        Assert.Contains("显示全部任务", markup, StringComparison.Ordinal);
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

        Assert.Equal(3, columns.Length);
        Assert.Equal("1", (string?)health.Attribute("Grid.Column"));
        Assert.Equal("{Binding DataSourceHealth}", (string?)health.Attribute("Details"));
        Assert.Equal("2", (string?)updated.Attribute("Grid.Column"));
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
}
