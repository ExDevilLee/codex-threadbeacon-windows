using ThreadBeacon.App.Sounds;

namespace ThreadBeacon.App.Tests.Sounds;

public sealed class WavSoundPlaybackServiceTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"threadbeacon-wav-player-{Guid.NewGuid():N}");

    [Theory]
    [InlineData(CompletionSound.Beacon, "Done-Beacon.wav")]
    [InlineData(CompletionSound.Chime, "Done-Chime.wav")]
    [InlineData(CompletionSound.Pulse, "Done-Pulse.wav")]
    public void GetSoundPath_MapsBundledSoundFile(
        CompletionSound sound,
        string expectedFileName)
    {
        var player = new WavSoundPlaybackService(tempRoot);

        string result = player.GetSoundPath(sound);

        Assert.Equal(
            Path.Combine(tempRoot, "Resources", "Sounds", expectedFileName),
            result);
    }

    [Fact]
    public void Play_MissingFileReturnsFalseWithoutThrowing()
    {
        var player = new WavSoundPlaybackService(tempRoot);

        bool result = player.Play(CompletionSound.Beacon);

        Assert.False(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
