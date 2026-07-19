using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Settings;

public interface IThreadListPreferenceStore
{
    ThreadListPreferences Load();

    bool Save(ThreadListPreferences preferences);
}
