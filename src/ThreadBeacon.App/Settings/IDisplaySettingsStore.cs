namespace ThreadBeacon.App.Settings;

public interface IDisplaySettingsStore
{
    DisplaySettings Load();

    bool Save(DisplaySettings settings);
}
