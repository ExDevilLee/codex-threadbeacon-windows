using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using ThreadBeacon.App.Commands;
using ThreadBeacon.App.Localization;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App.ViewModels;

public sealed class CompactionHookSettingsViewModel : INotifyPropertyChanged
{
    private readonly CompactionHookConfigurationManager manager;
    private readonly string helperSourcePath;
    private readonly AppLanguageState languageState;
    private CompactionHookConfigurationStatus status;
    private CompactionHookConfigurationError? lastError;

    public CompactionHookSettingsViewModel(
        CompactionHookConfigurationManager manager,
        string helperSourcePath,
        AppLanguageState languageState)
    {
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        this.helperSourcePath = Path.GetFullPath(helperSourcePath);
        this.languageState = languageState ?? throw new ArgumentNullException(nameof(languageState));
        status = manager.GetStatus();
        EnableCommand = new RelayCommand(Enable);
        RefreshCommand = new RelayCommand(Refresh);
        DisableCommand = new RelayCommand(Disable);
        languageState.Changed += OnLanguageChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public RelayCommand EnableCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand DisableCommand { get; }

    public CompactionHookConfigurationStatus Status
    {
        get => status;
        private set
        {
            if (status == value)
            {
                return;
            }
            status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string StatusText => (languageState.EffectiveLanguage, Status) switch
    {
        (AppLanguage.SimplifiedChinese, CompactionHookConfigurationStatus.Configured) => "\u5df2\u542f\u7528",
        (AppLanguage.SimplifiedChinese, CompactionHookConfigurationStatus.ExternallyModified) => "\u9700\u8981\u68c0\u67e5",
        (AppLanguage.SimplifiedChinese, _) => "\u672a\u914d\u7f6e",
        (_, CompactionHookConfigurationStatus.Configured) => "Enabled",
        (_, CompactionHookConfigurationStatus.ExternallyModified) => "Needs attention",
        _ => "Not configured",
    };

    public bool HasError => lastError is not null;

    public string ErrorText => lastError is null
        ? string.Empty
        : ErrorMessage(languageState.EffectiveLanguage, lastError.Value);

    private void Enable() => Run(() => Status = manager.Install(helperSourcePath));

    private void Refresh() => Run(() => Status = manager.GetStatus());

    private void Disable() => Run(() =>
    {
        manager.Uninstall();
        Status = manager.GetStatus();
    });

    private void Run(Action operation)
    {
        try
        {
            operation();
            SetError(null);
        }
        catch (CompactionHookConfigurationException error)
        {
            Status = manager.GetStatus();
            SetError(error.Error);
        }
    }

    private void SetError(CompactionHookConfigurationError? value)
    {
        if (lastError == value)
        {
            return;
        }
        lastError = value;
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorText));
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ErrorText));
    }

    private static string ErrorMessage(
        AppLanguage language,
        CompactionHookConfigurationError error)
    {
        if (language is AppLanguage.SimplifiedChinese)
        {
            return error switch
            {
                CompactionHookConfigurationError.InvalidHooksJson => "hooks.json \u4e0d\u662f\u6709\u6548\u7684 JSON\uff0c\u672a\u8fdb\u884c\u4fee\u6539\u3002",
                CompactionHookConfigurationError.UnsafeHooksFile => "hooks.json \u4e0d\u662f\u5b89\u5168\u7684\u666e\u901a\u6587\u4ef6\u3002",
                CompactionHookConfigurationError.InlineHooksPresent => "config.toml \u5df2\u5b9a\u4e49 Hooks\uff0c\u8bf7\u624b\u52a8\u5408\u5e76\u3002",
                CompactionHookConfigurationError.HelperUnavailable => "Hook Bridge \u7a0b\u5e8f\u4e0d\u53ef\u7528\u3002",
                CompactionHookConfigurationError.ConfigurationChanged => "\u914d\u7f6e\u521a\u88ab\u5176\u4ed6\u7a0b\u5e8f\u4fee\u6539\uff0c\u672a\u8986\u76d6\u8be5\u66f4\u6539\u3002",
                _ => "\u5199\u5165 Hook \u914d\u7f6e\u5931\u8d25\u3002",
            };
        }

        return error switch
        {
            CompactionHookConfigurationError.InvalidHooksJson => "hooks.json is not valid JSON and was not changed.",
            CompactionHookConfigurationError.UnsafeHooksFile => "hooks.json is not a safe regular file.",
            CompactionHookConfigurationError.InlineHooksPresent => "config.toml already defines Hooks; merge them manually.",
            CompactionHookConfigurationError.HelperUnavailable => "The Hook Bridge executable is unavailable.",
            CompactionHookConfigurationError.ConfigurationChanged => "Another program changed the configuration, so it was not overwritten.",
            _ => "The Hook configuration could not be written.",
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
