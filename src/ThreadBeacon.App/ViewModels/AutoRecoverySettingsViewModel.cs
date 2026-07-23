using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ThreadBeacon.App.AutoRecovery;
using ThreadBeacon.App.Commands;
using ThreadBeacon.App.Localization;
using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.ViewModels;

public sealed class AutoRecoveryRuleViewModel : INotifyPropertyChanged
{
    private readonly Action<AutoRecoveryRuleViewModel> changed;
    private bool isEnabled;
    private string prompt;
    private string displayName;
    private bool isCircuitBreakerEnabled;
    private int maximumConsecutiveAttempts;

    internal AutoRecoveryRuleViewModel(
        AutoRecoveryIncidentType type,
        AutoRecoveryRule rule,
        string displayName,
        Action<AutoRecoveryRuleViewModel> changed)
    {
        Type = type;
        isEnabled = rule.IsEnabled;
        prompt = rule.Prompt;
        isCircuitBreakerEnabled = rule.IsCircuitBreakerEnabled;
        maximumConsecutiveAttempts = rule.MaximumConsecutiveAttempts;
        this.displayName = displayName;
        this.changed = changed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AutoRecoveryIncidentType Type { get; }

    public string DisplayName
    {
        get => displayName;
        private set => SetField(ref displayName, value);
    }

    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (SetField(ref isEnabled, value))
            {
                changed(this);
            }
        }
    }

    public string Prompt
    {
        get => prompt;
        set
        {
            if (SetField(ref prompt, value))
            {
                changed(this);
            }
        }
    }

    public bool IsCircuitBreakerEnabled
    {
        get => isCircuitBreakerEnabled;
        set
        {
            if (SetField(ref isCircuitBreakerEnabled, value))
            {
                changed(this);
            }
        }
    }

    public int MaximumConsecutiveAttempts
    {
        get => maximumConsecutiveAttempts;
        set
        {
            int normalized = AutoRecoveryRule.ClampMaximum(value);
            if (SetField(ref maximumConsecutiveAttempts, normalized))
            {
                changed(this);
            }
        }
    }

    public IReadOnlyList<int> MaximumAttemptOptions { get; } = Enumerable.Range(
        AutoRecoveryRule.MinimumConsecutiveAttempts,
        AutoRecoveryRule.MaximumAllowedConsecutiveAttempts).ToArray();

    internal void Refresh(AutoRecoveryRule rule, string name)
    {
        isEnabled = rule.IsEnabled;
        prompt = rule.Prompt;
        isCircuitBreakerEnabled = rule.IsCircuitBreakerEnabled;
        maximumConsecutiveAttempts = rule.MaximumConsecutiveAttempts;
        DisplayName = name;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Prompt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCircuitBreakerEnabled)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaximumConsecutiveAttempts)));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

public sealed record AutoRecoveryHistoryRowViewModel(
    string ThreadId,
    string Incident,
    string Status,
    string UpdatedAt);

public sealed record AutoRecoveryCircuitRowViewModel(
    string ThreadId,
    string Incident,
    string Attempts,
    RelayCommand ResetCommand);

public sealed class AutoRecoverySettingsViewModel : INotifyPropertyChanged
{
    private readonly IAutoRecoverySettingsStore settingsStore;
    private readonly IAutoRecoveryHistoryStore historyStore;
    private readonly DisplaySettingsViewModel displaySettings;
    private readonly IAutoRecoveryCircuitStore? circuitStore;
    private readonly ObservableCollection<AutoRecoveryHistoryRowViewModel> history = [];
    private readonly ObservableCollection<AutoRecoveryCircuitRowViewModel> openCircuits = [];
    private AutoRecoverySettings settings;

    public AutoRecoverySettingsViewModel(
        IAutoRecoverySettingsStore settingsStore,
        IAutoRecoveryHistoryStore historyStore,
        DisplaySettingsViewModel displaySettings,
        IAutoRecoveryCircuitStore? circuitStore = null)
    {
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        this.historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
        this.displaySettings = displaySettings ?? throw new ArgumentNullException(nameof(displaySettings));
        this.circuitStore = circuitStore;
        settings = settingsStore.Load(PromptLanguage);
        Rules = Enum.GetValues<AutoRecoveryIncidentType>()
            .Select(type => new AutoRecoveryRuleViewModel(
                type,
                settings.RuleFor(type),
                IncidentName(type),
                OnRuleChanged))
            .ToArray();
        RefreshHistoryCommand = new RelayCommand(RefreshHistory);
        RefreshHistory();
        displaySettings.PropertyChanged += OnDisplaySettingsChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AutoRecoverySettings Settings => settings;

    public IReadOnlyList<AutoRecoveryRuleViewModel> Rules { get; }

    public ObservableCollection<AutoRecoveryHistoryRowViewModel> History => history;

    public ObservableCollection<AutoRecoveryCircuitRowViewModel> OpenCircuits => openCircuits;

    public RelayCommand RefreshHistoryCommand { get; }

    public bool IsEnabled
    {
        get => settings.IsEnabled;
        set
        {
            if (settings.IsEnabled == value)
            {
                return;
            }

            settings.IsEnabled = value;
            settingsStore.Save(settings);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }
    }

    public void RefreshHistory()
    {
        RefreshCircuits();
        history.Clear();
        foreach (AutoRecoveryHistoryEntry entry in historyStore.Load().Take(20))
        {
            history.Add(new AutoRecoveryHistoryRowViewModel(
                entry.ThreadId,
                IncidentName(entry.IncidentType),
                StatusName(entry.Status),
                entry.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")));
        }
    }

    private AutoRecoveryPromptLanguage PromptLanguage =>
        displaySettings.EffectiveLanguage is AppLanguage.English
            ? AutoRecoveryPromptLanguage.English
            : AutoRecoveryPromptLanguage.SimplifiedChinese;

    private void OnRuleChanged(AutoRecoveryRuleViewModel rule)
    {
        AutoRecoveryRule current = settings.RuleFor(rule.Type);
        AutoRecoveryPromptSource source = StringComparer.Ordinal.Equals(
            current.Prompt,
            rule.Prompt)
                ? current.PromptSource
                : AutoRecoveryPromptSource.Custom;
        settings.SetRule(
            rule.Type,
            new AutoRecoveryRule(
                rule.IsEnabled,
                rule.Prompt,
                source,
                rule.IsCircuitBreakerEnabled,
                rule.MaximumConsecutiveAttempts),
            PromptLanguage);
        AutoRecoveryRule normalized = settings.RuleFor(rule.Type);
        if (normalized.Prompt != rule.Prompt)
        {
            rule.Refresh(normalized, IncidentName(rule.Type));
        }

        settingsStore.Save(settings);
        RefreshCircuits();
    }

    private void OnDisplaySettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(DisplaySettingsViewModel.Language))
        {
            return;
        }

        settings.SynchronizeDefaultPrompts(PromptLanguage);
        settingsStore.Save(settings);
        foreach (AutoRecoveryRuleViewModel rule in Rules)
        {
            rule.Refresh(settings.RuleFor(rule.Type), IncidentName(rule.Type));
        }

        RefreshHistory();
    }

    private string IncidentName(AutoRecoveryIncidentType type) =>
        (displaySettings.EffectiveLanguage, type) switch
        {
            (AppLanguage.English, AutoRecoveryIncidentType.Http400) => "HTTP 400",
            (AppLanguage.English, AutoRecoveryIncidentType.Http429) => "HTTP 429",
            (AppLanguage.English, AutoRecoveryIncidentType.Http503) => "HTTP 503",
            (AppLanguage.English, AutoRecoveryIncidentType.OtherHttp) => "Other HTTP errors",
            (AppLanguage.English, AutoRecoveryIncidentType.ModelCapacity) => "Model capacity",
            (AppLanguage.English, _) => "Connection interrupted",
            (_, AutoRecoveryIncidentType.Http400) => "HTTP 400",
            (_, AutoRecoveryIncidentType.Http429) => "HTTP 429",
            (_, AutoRecoveryIncidentType.Http503) => "HTTP 503",
            (_, AutoRecoveryIncidentType.OtherHttp) => "其他 HTTP 错误",
            (_, AutoRecoveryIncidentType.ModelCapacity) => "模型容量限制",
            _ => "连接中断",
        };

    private string StatusName(AutoRecoveryHistoryStatus status) =>
        (displaySettings.EffectiveLanguage, status) switch
        {
            (AppLanguage.English, AutoRecoveryHistoryStatus.NotSent) => "Not sent",
            (AppLanguage.English, AutoRecoveryHistoryStatus.Sending) => "Sending",
            (AppLanguage.English, AutoRecoveryHistoryStatus.Sent) => "Sent",
            (AppLanguage.English, AutoRecoveryHistoryStatus.CircuitOpen) => "Circuit open",
            (AppLanguage.English, _) => "Failed",
            (_, AutoRecoveryHistoryStatus.NotSent) => "未发送",
            (_, AutoRecoveryHistoryStatus.Sending) => "发送中",
            (_, AutoRecoveryHistoryStatus.Sent) => "已发送",
            (_, AutoRecoveryHistoryStatus.CircuitOpen) => "已熔断",
            _ => "失败",
        };

    private void RefreshCircuits()
    {
        openCircuits.Clear();
        if (circuitStore is null)
        {
            return;
        }

        foreach (AutoRecoveryCircuitState state in circuitStore.Load())
        {
            AutoRecoveryRule rule = settings.RuleFor(state.IncidentType);
            if (!rule.IsCircuitBreakerEnabled
                || state.AttemptCount < rule.MaximumConsecutiveAttempts)
            {
                continue;
            }

            openCircuits.Add(new AutoRecoveryCircuitRowViewModel(
                state.ThreadId,
                IncidentName(state.IncidentType),
                $"{state.AttemptCount}/{rule.MaximumConsecutiveAttempts}",
                new RelayCommand(() =>
                {
                    circuitStore.Reset(state.ThreadId, state.IncidentType);
                    RefreshCircuits();
                })));
        }
    }
}
