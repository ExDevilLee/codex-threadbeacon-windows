namespace ThreadBeacon.Core.AutoRecovery;

public enum AutoRecoveryPromptLanguage
{
    SimplifiedChinese,
    English,
}

public enum AutoRecoveryPromptSource
{
    DefaultValue,
    Custom,
}

public enum AutoRecoveryIncidentType
{
    Http400,
    Http429,
    Http503,
    OtherHttp,
    ModelCapacity,
}

public sealed record AutoRecoveryRule(
    bool IsEnabled,
    string Prompt,
    AutoRecoveryPromptSource PromptSource = AutoRecoveryPromptSource.Custom);

public sealed class AutoRecoverySettings
{
    public const int MaximumPromptLength = 500;

    private readonly Dictionary<AutoRecoveryIncidentType, AutoRecoveryRule> rules;

    public AutoRecoverySettings(
        bool isEnabled,
        IReadOnlyDictionary<AutoRecoveryIncidentType, AutoRecoveryRule> rules)
    {
        IsEnabled = isEnabled;
        this.rules = new Dictionary<AutoRecoveryIncidentType, AutoRecoveryRule>(rules);
        foreach (AutoRecoveryIncidentType type in Enum.GetValues<AutoRecoveryIncidentType>())
        {
            if (!this.rules.ContainsKey(type))
            {
                this.rules[type] = DefaultRule(type, AutoRecoveryPromptLanguage.SimplifiedChinese);
            }
        }
    }

    public bool IsEnabled { get; set; }

    public IReadOnlyDictionary<AutoRecoveryIncidentType, AutoRecoveryRule> Rules => rules;

    public static AutoRecoverySettings CreateDefault(AutoRecoveryPromptLanguage language) => new(
        false,
        Enum.GetValues<AutoRecoveryIncidentType>().ToDictionary(
            type => type,
            type => DefaultRule(type, language)));

    public AutoRecoveryRule RuleFor(AutoRecoveryIncidentType type) => rules[type];

    public void SetRule(
        AutoRecoveryIncidentType type,
        AutoRecoveryRule rule,
        AutoRecoveryPromptLanguage fallbackLanguage = AutoRecoveryPromptLanguage.SimplifiedChinese)
    {
        ArgumentNullException.ThrowIfNull(rule);
        string normalized = rule.Prompt.Trim();
        rules[type] = normalized.Length is > 0 and <= MaximumPromptLength
            ? rule with { Prompt = normalized }
            : DefaultRule(type, fallbackLanguage);
    }

    public void SynchronizeDefaultPrompts(AutoRecoveryPromptLanguage language)
    {
        foreach (AutoRecoveryIncidentType type in Enum.GetValues<AutoRecoveryIncidentType>())
        {
            AutoRecoveryRule current = rules[type];
            if (current.PromptSource is AutoRecoveryPromptSource.DefaultValue)
            {
                AutoRecoveryRule localized = DefaultRule(type, language);
                rules[type] = localized with { IsEnabled = current.IsEnabled };
            }
        }
    }

    public static AutoRecoveryRule DefaultRule(
        AutoRecoveryIncidentType type,
        AutoRecoveryPromptLanguage language) => new(
            type is not AutoRecoveryIncidentType.Http503,
            DefaultPrompt(type, language),
            AutoRecoveryPromptSource.DefaultValue);

    private static string DefaultPrompt(
        AutoRecoveryIncidentType type,
        AutoRecoveryPromptLanguage language) => (language, type) switch
    {
        (AutoRecoveryPromptLanguage.SimplifiedChinese, AutoRecoveryIncidentType.Http400) =>
            "刚才请求异常中断了，请继续未完成的任务",
        (AutoRecoveryPromptLanguage.SimplifiedChinese, AutoRecoveryIncidentType.Http429) =>
            "刚才请求频率受限并已中断，请继续未完成的任务",
        (AutoRecoveryPromptLanguage.SimplifiedChinese, AutoRecoveryIncidentType.Http503) =>
            "刚才服务暂时不可用并已中断，请继续未完成的任务",
        (AutoRecoveryPromptLanguage.SimplifiedChinese, AutoRecoveryIncidentType.OtherHttp) =>
            "刚才请求异常中断了，请继续未完成的任务",
        (AutoRecoveryPromptLanguage.SimplifiedChinese, AutoRecoveryIncidentType.ModelCapacity) =>
            "刚才因模型容量限制中断了，请继续未完成的任务",
        (AutoRecoveryPromptLanguage.English, AutoRecoveryIncidentType.Http400) =>
            "The previous request was interrupted by an error. Please continue the unfinished task.",
        (AutoRecoveryPromptLanguage.English, AutoRecoveryIncidentType.Http429) =>
            "The previous request was interrupted by rate limiting. Please continue the unfinished task.",
        (AutoRecoveryPromptLanguage.English, AutoRecoveryIncidentType.Http503) =>
            "The previous request was interrupted because the service was unavailable. Please continue the unfinished task.",
        (AutoRecoveryPromptLanguage.English, AutoRecoveryIncidentType.OtherHttp) =>
            "The previous request was interrupted by an HTTP error. Please continue the unfinished task.",
        _ => "The previous request was interrupted due to model capacity limits. Please continue the unfinished task.",
    };
}
