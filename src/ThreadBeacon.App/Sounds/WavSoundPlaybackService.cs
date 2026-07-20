using System.IO;
using System.Media;

namespace ThreadBeacon.App.Sounds;

public sealed class WavSoundPlaybackService : ISoundPlaybackService, IDisposable
{
    private readonly string baseDirectory;
    private readonly Func<string, bool> pathPlayer;
    private SoundPlayer? activePlayer;

    public WavSoundPlaybackService(
        string? baseDirectory = null,
        Func<string, bool>? pathPlayer = null)
    {
        this.baseDirectory = baseDirectory ?? AppContext.BaseDirectory;
        this.pathPlayer = pathPlayer ?? TryPlayPath;
    }

    public string GetSoundPath(CompletionSound sound)
    {
        string fileName = sound switch
        {
            CompletionSound.FupicatNotification => "Done-Fupicat-Notification.wav",
            CompletionSound.BassguitarNotification => "Done-Bassguitar-Notification.wav",
            CompletionSound.Beacon => "Done-Beacon.wav",
            CompletionSound.Chime => "Done-Chime.wav",
            CompletionSound.Pulse => "Done-Pulse.wav",
            CompletionSound.Alert => "Done-Alert.wav",
            CompletionSound.Resolve => "Done-Resolve.wav",
            CompletionSound.Knock => "Done-Knock.wav",
            _ => throw new ArgumentOutOfRangeException(nameof(sound)),
        };

        return Path.Combine(baseDirectory, "Resources", "Sounds", fileName);
    }

    public bool Play(CompletionSound sound)
        => Play(sound, customPath: null);

    public bool Play(CompletionSound sound, string? customPath)
    {
        string builtInPath = GetSoundPath(sound);
        if (!string.IsNullOrWhiteSpace(customPath)
            && File.Exists(customPath)
            && pathPlayer(customPath))
        {
            return true;
        }

        return File.Exists(builtInPath) && pathPlayer(builtInPath);
    }

    private bool TryPlayPath(string path)
    {
        try
        {
            var player = new SoundPlayer(path);
            player.Load();
            activePlayer?.Stop();
            activePlayer?.Dispose();
            activePlayer = player;
            activePlayer.Play();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        activePlayer?.Stop();
        activePlayer?.Dispose();
        activePlayer = null;
    }
}
