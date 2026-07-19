namespace ThreadBeacon.App.Sounds;

public interface ISoundNotificationSettingsStore
{
    SoundNotificationSettings Load();

    bool Save(SoundNotificationSettings settings);
}
