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
}
