using System.Globalization;
using System.Windows;

namespace ThreadBeacon.App.Localization;

public sealed class AppLanguageState
{
    private readonly string? systemCultureName;
    private AppLanguage preference;

    public AppLanguageState(
        AppLanguage preference,
        string? systemCultureName = null,
        Action<AppLanguage>? persist = null)
    {
        this.preference = preference;
        this.systemCultureName = systemCultureName;
        Persist = persist;
    }

    public event EventHandler? Changed;

    public Action<AppLanguage>? Persist { get; }

    public AppLanguage Preference => preference;

    public AppLanguage EffectiveLanguage => preference is AppLanguage.System
        ? AppLanguageResolver.ResolveSystem(systemCultureName)
        : preference;

    public void SetPreference(AppLanguage value)
    {
        if (preference == value)
        {
            return;
        }

        preference = value;
        Persist?.Invoke(value);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyResources(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        string language = EffectiveLanguage is AppLanguage.SimplifiedChinese
            ? "zh-Hans"
            : "en";
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                $"pack://application:,,,/ThreadBeacon.App;component/Resources/Strings.{language}.xaml",
                UriKind.Absolute),
        };
        ResourceDictionary? previous = application.Resources.MergedDictionaries
            .FirstOrDefault(candidate => candidate.Source?.OriginalString.Contains(
                "/Resources/Strings.", StringComparison.Ordinal) is true);
        if (previous is not null)
        {
            application.Resources.MergedDictionaries.Remove(previous);
        }

        application.Resources.MergedDictionaries.Add(dictionary);
    }
}
