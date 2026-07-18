namespace ThreadBeacon.App.Settings;

public interface IAppSettingsStore
{
    AppSettings Load();

    bool Save(AppSettings settings);
}
