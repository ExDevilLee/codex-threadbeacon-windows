using ThreadBeacon.App.Windowing;

namespace ThreadBeacon.App.Tests.Windowing;

public sealed class JsonWindowPlacementStoreTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"ThreadBeacon.WindowPlacement.Tests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoad_RoundTripsPlacement()
    {
        var placement = new WindowPlacement(
            @"\\.\DISPLAY2",
            new WindowBounds(-620, 80, 620, 500));
        JsonWindowPlacementStore store = Store();

        bool saved = store.Save(placement);

        Assert.True(saved);
        Assert.Equal(placement, store.Load());
    }

    [Fact]
    public void Load_WhenFileIsMissingOrMalformed_ReturnsNull()
    {
        JsonWindowPlacementStore store = Store();

        Assert.Null(store.Load());

        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(Path.Combine(tempDirectory, "window-placement.json"), "not-json");
        Assert.Null(store.Load());
    }

    [Fact]
    public void Save_WhenParentPathIsAFile_ReturnsFalse()
    {
        Directory.CreateDirectory(tempDirectory);
        string blockedParent = Path.Combine(tempDirectory, "blocked");
        File.WriteAllText(blockedParent, "blocked");
        var store = new JsonWindowPlacementStore(
            Path.Combine(blockedParent, "window-placement.json"));

        bool saved = store.Save(new WindowPlacement(
            @"\\.\DISPLAY1",
            new WindowBounds(0, 0, 620, 500)));

        Assert.False(saved);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private JsonWindowPlacementStore Store() => new(
        Path.Combine(tempDirectory, "window-placement.json"));
}
