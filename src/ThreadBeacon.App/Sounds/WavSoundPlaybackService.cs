using System.IO;
using System.Media;

namespace ThreadBeacon.App.Sounds;

public sealed class WavSoundPlaybackService : ISoundPlaybackService, IDisposable
{
    private readonly string baseDirectory;
    private SoundPlayer? activePlayer;

    public WavSoundPlaybackService(string? baseDirectory = null)
    {
        this.baseDirectory = baseDirectory ?? AppContext.BaseDirectory;
    }

    public string GetSoundPath(CompletionSound sound)
    {
        string fileName = sound switch
        {
            CompletionSound.Beacon => "Done-Beacon.wav",
            CompletionSound.Chime => "Done-Chime.wav",
            CompletionSound.Pulse => "Done-Pulse.wav",
            _ => throw new ArgumentOutOfRangeException(nameof(sound)),
        };

        return Path.Combine(baseDirectory, "Resources", "Sounds", fileName);
    }

    public bool Play(CompletionSound sound)
    {
        try
        {
            string soundPath = GetSoundPath(sound);
            if (!File.Exists(soundPath))
            {
                return false;
            }

            var player = new SoundPlayer(soundPath);
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
