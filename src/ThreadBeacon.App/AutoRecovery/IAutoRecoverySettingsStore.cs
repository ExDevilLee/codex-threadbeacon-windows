using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.AutoRecovery;

public interface IAutoRecoverySettingsStore
{
    AutoRecoverySettings Load(AutoRecoveryPromptLanguage language);

    bool Save(AutoRecoverySettings settings);
}
