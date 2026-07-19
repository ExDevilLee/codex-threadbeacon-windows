namespace ThreadBeacon.App.Tests.Views;

public sealed class MainWindowPlacementWiringTests
{
    private static string LoadCodeBehind() => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "MainWindow.xaml.cs.txt"));

    [Fact]
    public void MainWindow_RestoresAfterHandleCreationAndTracksNormalBounds()
    {
        string source = LoadCodeBehind();

        Assert.Contains("WindowPlacementCoordinator", source, StringComparison.Ordinal);
        Assert.Contains("SourceInitialized += OnSourceInitialized", source, StringComparison.Ordinal);
        Assert.Contains("LocationChanged += OnMainWindowBoundsChanged", source, StringComparison.Ordinal);
        Assert.Contains("SizeChanged += OnMainWindowBoundsChanged", source, StringComparison.Ordinal);
        Assert.Contains("new WindowInteropHelper(this).Handle", source, StringComparison.Ordinal);
        Assert.Contains("WindowState is WindowState.Normal", source, StringComparison.Ordinal);
        Assert.Contains("isPlacementTrackingActive", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MainAndSettingsWindows_KeepExpectedFallbackStartupLocations()
    {
        string mainXaml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "MainWindow.xaml"));
        string settingsXaml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "SettingsWindow.xaml"));

        Assert.Contains("WindowStartupLocation=\"CenterScreen\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("WindowStartupLocation=\"CenterOwner\"", settingsXaml, StringComparison.Ordinal);
    }
}
