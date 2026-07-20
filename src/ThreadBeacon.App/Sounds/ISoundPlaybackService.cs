namespace ThreadBeacon.App.Sounds;

public interface ISoundPlaybackService
{
    bool Play(CompletionSound sound);

    bool Play(CompletionSound sound, string? customPath) => Play(sound);
}
