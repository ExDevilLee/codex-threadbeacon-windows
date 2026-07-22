namespace ThreadBeacon.App.Localization;

using ThreadBeacon.Core.Models;
using ThreadBeacon.App.Theme;

public static class AppLanguageText
{
    public static string RefreshSeconds(AppLanguage language, int value) => language switch
    {
        AppLanguage.SimplifiedChinese => $"{value} 秒",
        _ => $"{value} sec",
    };

    public static string TaskCount(AppLanguage language, int value) => language switch
    {
        AppLanguage.SimplifiedChinese => $"{value} 个",
        _ => $"{value} tasks",
    };

    public static string LanguageName(AppLanguage language) => language switch
    {
        AppLanguage.SimplifiedChinese => "简体中文",
        AppLanguage.English => "English",
        _ => "跟随系统 / System",
    };

    public static string ThemeName(AppLanguage language, AppTheme theme) =>
        language is AppLanguage.SimplifiedChinese
            ? theme switch
            {
                AppTheme.Light => "浅色",
                AppTheme.Dark => "深色",
                _ => "跟随系统",
            }
            : theme switch
            {
                AppTheme.Light => "Light",
                AppTheme.Dark => "Dark",
                _ => "Follow system / System",
            };

    public static string Status(AppLanguage language, ThreadStatus status) => language switch
    {
        AppLanguage.SimplifiedChinese => status switch
        {
            ThreadStatus.Error => "错误",
            ThreadStatus.NeedsAction => "待处理",
            ThreadStatus.Warning => "重试中",
            ThreadStatus.Running => "运行中",
            ThreadStatus.Interrupted => "已中断",
            ThreadStatus.JustCompleted => "刚完成",
            ThreadStatus.Idle => "空闲",
            _ => "未知",
        },
        _ => status switch
        {
            ThreadStatus.Error => "Error",
            ThreadStatus.NeedsAction => "Action",
            ThreadStatus.Warning => "Retrying",
            ThreadStatus.Running => "Running",
            ThreadStatus.Interrupted => "Interrupted",
            ThreadStatus.JustCompleted => "Done",
            ThreadStatus.Idle => "Idle",
            _ => "Unknown",
        },
    };

    public static string Duration(AppLanguage language, TimeSpan elapsed)
    {
        elapsed = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        double value;
        string unit;
        if (elapsed.TotalSeconds < 60)
        {
            value = Math.Floor(elapsed.TotalSeconds);
            unit = language is AppLanguage.SimplifiedChinese ? "秒" : "sec";
        }
        else if (elapsed.TotalSeconds < 3_600)
        {
            value = Math.Floor(elapsed.TotalMinutes);
            unit = language is AppLanguage.SimplifiedChinese ? "分" : "min";
        }
        else if (elapsed.TotalSeconds < 86_400)
        {
            value = Math.Floor(elapsed.TotalHours);
            unit = language is AppLanguage.SimplifiedChinese ? "时" : "hr";
        }
        else
        {
            value = Math.Floor(elapsed.TotalDays);
            unit = language is AppLanguage.SimplifiedChinese ? "天" : "day";
        }

        if (language is AppLanguage.English && unit == "day" && value != 1)
        {
            unit = "days";
        }

        return language is AppLanguage.SimplifiedChinese
            ? $"{value}{unit}"
            : $"{value} {unit}";
    }

    public static string PinCommand(AppLanguage language, bool isPinned) =>
        language is AppLanguage.SimplifiedChinese
            ? isPinned ? "取消置顶" : "置顶任务"
            : isPinned ? "Unpin task" : "Pin task";

    public static string FavoriteCommand(AppLanguage language, bool isFavorite) =>
        language is AppLanguage.SimplifiedChinese
            ? isFavorite ? "取消收藏" : "收藏任务"
            : isFavorite ? "Remove favorite" : "Favorite task";

    public static string SubagentCount(AppLanguage language, int activeCount, int totalCount) =>
        language is AppLanguage.SimplifiedChinese
            ? $"运行中 {activeCount} 个，共 {totalCount} 个 Subagent"
            : $"{activeCount} running, {totalCount} Subagents total";

    public static string SubagentToggle(
        AppLanguage language,
        int activeCount,
        int totalCount,
        bool isExpanded) =>
        language is AppLanguage.SimplifiedChinese
            ? $"{SubagentCount(language, activeCount, totalCount)}；点击{(isExpanded ? "收起" : "展开")}"
            : $"{SubagentCount(language, activeCount, totalCount)}; click to {(isExpanded ? "collapse" : "expand")}";

    public static string HealthSummary(AppLanguage language, OverallDataSourceHealth status) =>
        language is AppLanguage.SimplifiedChinese
            ? status switch
            {
                OverallDataSourceHealth.Healthy => "数据源正常",
                OverallDataSourceHealth.Degraded => "部分数据源降级",
                _ => "任务数据不可用",
            }
            : status switch
            {
                OverallDataSourceHealth.Healthy => "Data sources healthy",
                OverallDataSourceHealth.Degraded => "Some data sources degraded",
                _ => "Task data unavailable",
            };

    public static string HealthStatus(AppLanguage language, DataSourceHealthLevel level) =>
        language is AppLanguage.SimplifiedChinese
            ? level switch
            {
                DataSourceHealthLevel.Healthy => "正常",
                DataSourceHealthLevel.Degraded => "部分降级",
                DataSourceHealthLevel.Unavailable => "不可用",
                _ => "未使用",
            }
            : level switch
            {
                DataSourceHealthLevel.Healthy => "Healthy",
                DataSourceHealthLevel.Degraded => "Degraded",
                DataSourceHealthLevel.Unavailable => "Unavailable",
                _ => "Not used",
            };

    public static string HealthSourceTitle(AppLanguage language, int index) =>
        language is AppLanguage.SimplifiedChinese
            ? new[] { "任务数据库", "Rename 索引", "Rollout", "服务日志" }[index]
            : new[] { "Task database", "Rename index", "Rollout", "Service logs" }[index];

    public static string TokenLabel(AppLanguage language, int index) =>
        language is AppLanguage.SimplifiedChinese
            ? new[] { "会话总量", "输入", "缓存输入", "非缓存输入", "输出", "Reasoning", "当前 turn", "缓存率", "更新时间" }[index]
            : new[] { "Session total", "Input", "Cached input", "Uncached input", "Output", "Reasoning", "Current turn", "Cache ratio", "Updated" }[index];

    #if false
    public static string CompactionLabel(AppLanguage language, int index) =>
        language is AppLanguage.SimplifiedChinese
            ? new[] { "鍘嬬缉娆℃暟", "鏈€杩戝帇缁? }[index]
            : new[] { "Compactions", "Last compaction" }[index];
    #endif

    public static string CompactionLabel(AppLanguage language, int index) =>
        language is AppLanguage.SimplifiedChinese
            ? new[] { "\u538b\u7f29\u6b21\u6570", "\u6700\u8fd1\u538b\u7f29" }[index]
            : new[] { "Compactions", "Last compaction" }[index];

    public static string Compacting(AppLanguage language) =>
        language is AppLanguage.SimplifiedChinese ? "\u538b\u7f29\u4e2d" : "Compacting";

    public static string TaskMetadataLabel(AppLanguage language, int index) =>
        language is AppLanguage.SimplifiedChinese
            ? new[] { "模型", "推理强度" }[index]
            : new[] { "Model", "Reasoning" }[index];

    public static string ReasoningEffort(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "—";
        }

        string normalized = value.Trim();
        return normalized.ToLowerInvariant() switch
        {
            "xhigh" => "XHigh",
            "high" => "High",
            "medium" => "Medium",
            "low" => "Low",
            "minimal" => "Minimal",
            "none" => "None",
            _ => normalized,
        };
    }

    public static string TokenNote(AppLanguage language) =>
        language is AppLanguage.SimplifiedChinese
            ? "缓存输入已包含在输入中；Reasoning 已包含在输出中。"
            : "Cached input is included in input; Reasoning is included in output.";

    public static string ThreadCountAccessibility(
        AppLanguage language,
        int runningCount,
        int visibleCount) => language is AppLanguage.SimplifiedChinese
        ? $"{runningCount} 个任务正在运行，共显示 {visibleCount} 个任务"
        : $"{runningCount} tasks running, {visibleCount} tasks visible";

    public static string MonitoringSummary(AppLanguage language, int taskCount, bool degraded) =>
        language is AppLanguage.SimplifiedChinese
            ? $"监听中 · {taskCount} 个任务{(degraded ? " · 部分数据降级" : string.Empty)}"
            : $"Monitoring · {taskCount} tasks{(degraded ? " · Some data degraded" : string.Empty)}";

    public static string SubagentDetailLabel(AppLanguage language, int index) =>
        language is AppLanguage.SimplifiedChinese
            ? new[] { "状态", "Agent", "角色", "模型", "Reasoning", "累计 Token", "输入", "缓存输入", "输出", "Reasoning Token", "当前 turn", "最近活动" }[index]
            : new[] { "Status", "Agent", "Role", "Model", "Reasoning", "Total Token", "Input", "Cached input", "Output", "Reasoning Token", "Current turn", "Latest activity" }[index];

    public static string HealthDetail(AppLanguage language, string? detail)
    {
        if (language is AppLanguage.SimplifiedChinese || string.IsNullOrEmpty(detail))
        {
            return detail ?? string.Empty;
        }

        return detail switch
        {
            "未找到 Codex 任务数据库" => "Codex task database not found",
            "Codex 任务数据库正忙" => "Codex task database is busy",
            "Codex 任务数据库格式暂不兼容" => "Codex task database format is not supported",
            "Codex 任务数据库暂不可用" => "Codex task database unavailable",
            "未找到 Rename 索引" => "Rename index not found",
            "Rename 索引格式暂不兼容" => "Rename index format is not supported",
            "Rename 索引暂不可用" => "Rename index unavailable",
            "Rollout 数据不可用" => "Rollout data unavailable",
            "部分 Rollout 无法读取" => "Some Rollout files could not be read",
            "未找到服务日志数据库" => "Service log database not found",
            "服务日志数据库正忙" => "Service log database is busy",
            "服务日志数据库格式暂不兼容" => "Service log database format is not supported",
            "服务日志数据库暂不可用" => "Service log database unavailable",
            _ => detail,
        };
    }
}
