using ThreadBeacon.App.Sounds;

namespace ThreadBeacon.App.Tests.Sounds;

public sealed class WavSoundPlaybackServiceTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"threadbeacon-wav-player-{Guid.NewGuid():N}");

    [Theory]
    [InlineData(CompletionSound.FupicatNotification, "Done-Fupicat-Notification.wav")]
    [InlineData(CompletionSound.BassguitarNotification, "Done-Bassguitar-Notification.wav")]
    [InlineData(CompletionSound.Beacon, "Done-Beacon.wav")]
    [InlineData(CompletionSound.Chime, "Done-Chime.wav")]
    [InlineData(CompletionSound.Pulse, "Done-Pulse.wav")]
    [InlineData(CompletionSound.Alert, "Done-Alert.wav")]
    [InlineData(CompletionSound.Resolve, "Done-Resolve.wav")]
    [InlineData(CompletionSound.Knock, "Done-Knock.wav")]
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

    [Fact]
    public void Play_InvalidCustomFileFallsBackToBundledSound()
    {
        string soundDirectory = Path.Combine(tempRoot, "Resources", "Sounds");
        Directory.CreateDirectory(soundDirectory);
        File.WriteAllBytes(Path.Combine(soundDirectory, "Done-Beacon.wav"), [1]);
        string invalidCustom = Path.Combine(tempRoot, "invalid.wav");
        File.WriteAllText(invalidCustom, "not a WAV file");
        var attempted = new List<string>();
        var player = new WavSoundPlaybackService(
            tempRoot,
            path =>
            {
                attempted.Add(path);
                return path.EndsWith("Done-Beacon.wav", StringComparison.Ordinal);
            });

        bool result = player.Play(CompletionSound.Beacon, invalidCustom);

        Assert.True(result);
        Assert.Equal([invalidCustom, Path.Combine(soundDirectory, "Done-Beacon.wav")], attempted);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
